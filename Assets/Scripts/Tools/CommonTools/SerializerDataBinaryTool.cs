using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;


namespace ZTools
{ 
/// <summary>
/// 数据序列化二进制
/// 子类，基类特性ID具有唯一性
/// 基类特性ID 1 - 99
/// 非基类特性ID 101 - 999
/// </summary>
    public static class SerializerDataBinaryTool
    {
        // 字段类型枚举（已声明，无需修改）
        private enum FieldType : byte
        {
            // 基础类型
            Int32 = 1,
            Int64 = 2,
            Float = 3,
            Double = 4,
            String = 5,
            Bool = 6,


            // List 类型
            ListInt32 = 10,
            ListString = 11,
            ListVector3 = 12,
            ListCustom = 13,

            // 自定义类（含子类继承基类）
            CustomClass = 20
        }

        // 缓存：字段ID唯一性校验（全局唯一，避免基类和子类ID冲突）
        private static readonly HashSet<int> _globalFieldIds = new HashSet<int>();

        // 序列化（支持基类字段）
        public static byte[] Serialize(object obj)
        {
            if (obj == null) throw new ArgumentNullException("序列化对象不能为null");

            // 校验所有字段ID唯一性（基类+子类）
            ValidateFieldIds(obj.GetType());

            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                SerializeObject(writer, obj);
                return ms.ToArray();
            }
        }

        // 反序列化（支持基类字段）
        public static T Deserialize<T>(byte[] data) where T : new()
        {
            // 校验所有字段ID唯一性（基类+子类）
            ValidateFieldIds(typeof(T));

            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                return (T)DeserializeObject(reader, typeof(T));
            }
        }

        // 核心：序列化单个对象（递归处理基类字段+补全所有类型）
        private static void SerializeObject(BinaryWriter writer, object obj)
        {
            if (obj == null) throw new ArgumentNullException("序列化对象不能为null");

            Type objType = obj.GetType();
            FieldType fieldType = GetFieldType(objType);
            writer.Write((byte)fieldType);

            switch (fieldType)
            {
                // 补全所有基础类型序列化
                case FieldType.Int32: writer.Write((int)obj); break;
                case FieldType.Int64: writer.Write((long)obj); break;
                case FieldType.Float: writer.Write((float)obj); break;
                case FieldType.Double: writer.Write((double)obj); break;
                case FieldType.String: writer.Write((string)obj); break;
                case FieldType.Bool: writer.Write((bool)obj); break;
               

                // 补全所有基础类型List序列化
                case FieldType.ListInt32: SerializeList(writer, (List<int>)obj); break;
                case FieldType.ListString: SerializeList(writer, (List<string>)obj); break;
               
                case FieldType.ListCustom: SerializeCustomList(writer, obj as IList); break;

                // 自定义类（含子类）：递归读取基类+当前类的字段
                case FieldType.CustomClass:
                    SerializeCustomClassWithBase(writer, obj);
                    break;
            }
        }

        // 核心：反序列化单个对象（递归处理基类字段+补全所有类型）
        private static object DeserializeObject(BinaryReader reader, Type targetType)
        {
            FieldType fieldType = (FieldType)reader.ReadByte();

            switch (fieldType)
            {
                // 补全所有基础类型反序列化
                case FieldType.Int32: return reader.ReadInt32();
                case FieldType.Int64: return reader.ReadInt64();
                case FieldType.Float: return reader.ReadSingle();
                case FieldType.Double: return reader.ReadDouble();
                case FieldType.String: return reader.ReadString();
                case FieldType.Bool: return reader.ReadBoolean();
               

                // 补全所有基础类型List反序列化
                case FieldType.ListInt32: return DeserializeList<int>(reader);
                case FieldType.ListString: return DeserializeList<string>(reader);
               
                case FieldType.ListCustom: return DeserializeCustomList(reader, targetType);

                // 自定义类（含子类）：递归初始化基类+当前类的字段
                case FieldType.CustomClass:
                    return DeserializeCustomClassWithBase(reader, targetType);
            }

            throw new NotSupportedException($"不支持反序列化类型：{targetType.Name}");
        }

        // 关键：序列化自定义类（含基类字段）
        private static void SerializeCustomClassWithBase(BinaryWriter writer, object obj)
        {
            Type objType = obj.GetType();
            // 递归获取：基类 + 当前类的所有[BinaryField]字段（按ID排序）
            var allFields = GetAllSerializableFieldsWithBase(objType);
            allFields.Sort((a, b) =>
            {
                int idA = a.GetCustomAttribute<SerializerDataFieldAttribute>().FieldId;
                int idB = b.GetCustomAttribute<SerializerDataFieldAttribute>().FieldId;
                return idA.CompareTo(idB);
            });

            writer.Write(allFields.Count); // 写入总字段数（基类+当前类）

            foreach (var field in allFields)
            {
                SerializerDataFieldAttribute attr = field.GetCustomAttribute<SerializerDataFieldAttribute>();
                object fieldValue = field.GetValue(obj); // 基类字段通过反射直接获取（public字段）

                WriteVarint(writer, attr.FieldId); // 写入字段ID
                SerializeObject(writer, fieldValue ?? GetDefaultValue(field)); // 递归序列化字段值
            }
        }

        // 关键：反序列化自定义类（含基类字段）
        private static object DeserializeCustomClassWithBase(BinaryReader reader, Type targetType)
        {
            object obj = Activator.CreateInstance(targetType); // 创建子类实例（基类会自动初始化）
            // 递归获取：基类 + 当前类的所有[BinaryField]字段（ID→FieldInfo映射）
            var allFieldMap = GetAllFieldIdMapWithBase(targetType);

            int fieldCount = reader.ReadInt32(); // 读取总字段数（基类+当前类）
            for (int i = 0; i < fieldCount; i++)
            {
                int fieldId = ReadVarint(reader);
                if (allFieldMap.TryGetValue(fieldId, out FieldInfo field))
                {
                    // 递归反序列化字段值（基类字段直接赋值给子类实例）
                    object fieldValue = DeserializeObject(reader, field.FieldType);
                    field.SetValue(obj, fieldValue ?? GetDefaultValue(field));
                }
                else
                {
                    SkipObject(reader); // 跳过已删除的字段
                }
            }

            return obj;
        }

        // 递归获取：当前类 + 所有基类的[BinaryField]字段（直到object）
        private static List<FieldInfo> GetAllSerializableFieldsWithBase(Type type)
        {
            var fields = new List<FieldInfo>();

            // 递归向上遍历基类（终止条件：type == typeof(object)）
            if (type.BaseType != null && type.BaseType != typeof(object))
            {
                fields.AddRange(GetAllSerializableFieldsWithBase(type.BaseType));
            }

            // 添加当前类的[BinaryField]字段
            fields.AddRange(GetSerializableFields(type));

            return fields;
        }

        // 递归构建：当前类 + 所有基类的FieldId→FieldInfo映射
        private static Dictionary<int, FieldInfo> GetAllFieldIdMapWithBase(Type type)
        {
            var fieldMap = new Dictionary<int, FieldInfo>();

            // 递归向上遍历基类（终止条件：type == typeof(object)）
            if (type.BaseType != null && type.BaseType != typeof(object))
            {
                var baseMap = GetAllFieldIdMapWithBase(type.BaseType);
                foreach (var kvp in baseMap)
                {
                    fieldMap[kvp.Key] = kvp.Value; // 基类字段加入映射
                }
            }

            // 添加当前类的[BinaryField]字段映射
            var currentMap = GetFieldIdMap(type);
            foreach (var kvp in currentMap)
            {
                fieldMap[kvp.Key] = kvp.Value; // 当前类字段加入映射（若ID冲突，会在Validate时报错）
            }

            return fieldMap;
        }

        // 校验：当前类+基类的所有[BinaryField]ID全局唯一（避免冲突）
        private static void ValidateFieldIds(Type type)
        {
            _globalFieldIds.Clear(); // 清空缓存
            var allFields = GetAllSerializableFieldsWithBase(type);

            foreach (var field in allFields)
            {
                int fieldId = field.GetCustomAttribute<SerializerDataFieldAttribute>().FieldId;
                if (_globalFieldIds.Contains(fieldId))
                {
                    throw new InvalidOperationException($"字段ID冲突：ID={fieldId} 已在 {type.Name} 或其基类中使用");
                }
                _globalFieldIds.Add(fieldId);
            }
        }

        private static List<FieldInfo> GetSerializableFields(Type type)
        {
            var fields = new List<FieldInfo>();
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.GetCustomAttribute<SerializerDataFieldAttribute>() != null)
                    fields.Add(field);
            }
            return fields;
        }

        private static Dictionary<int, FieldInfo> GetFieldIdMap(Type type)
        {
            var map = new Dictionary<int, FieldInfo>();
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var attr = field.GetCustomAttribute<SerializerDataFieldAttribute>();
                if (attr != null) map[attr.FieldId] = field;
            }
            return map;
        }

        private static object GetDefaultValue(FieldInfo field)
        {
            var attr = field.GetCustomAttribute<SerializerDataFieldAttribute>();
            if (attr?.DefaultValue != null) return attr.DefaultValue;
            return GetDefaultValue(field.FieldType);
        }

        private static object GetDefaultValue(Type type)
        {
            if (type.IsValueType) return Activator.CreateInstance(type);
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return Activator.CreateInstance(type);
            if (type.IsDefined(typeof(SerializableAttribute), false) && HasBinaryField(type))
                return Activator.CreateInstance(type);
            return null;
        }

        private static bool HasBinaryField(Type type)
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.GetCustomAttribute<SerializerDataFieldAttribute>() != null)
                    return true;
            }
            return false;
        }

        // 补全：GetFieldType（支持所有枚举中声明的类型+List）
        private static FieldType GetFieldType(Type type)
        {
            // 基础类型（补全所有枚举声明的类型）
            if (type == typeof(int)) return FieldType.Int32;
            if (type == typeof(long)) return FieldType.Int64;
            if (type == typeof(float)) return FieldType.Float;
            if (type == typeof(double)) return FieldType.Double;
            if (type == typeof(string)) return FieldType.String;
            if (type == typeof(bool)) return FieldType.Bool;
           

            bool isSerializable = type.IsDefined(typeof(SerializableAttribute), false);
            bool hasBinaryField = HasBinaryField(type);
            // 自定义类（含子类，判断是否有[Serializable]和[BinaryField]）
            if (isSerializable && hasBinaryField)
            {
                return FieldType.CustomClass;
            }

            // List 类型（补全所有枚举声明的List类型）
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type elementType = type.GetGenericArguments()[0];
                if (elementType == typeof(int)) return FieldType.ListInt32;
                if (elementType == typeof(string)) return FieldType.ListString;
               
                // 自定义类List（元素是带[Serializable]+[BinaryField]的类）
                if (elementType.IsDefined(typeof(SerializableAttribute), false) && HasBinaryField(elementType))
                    return FieldType.ListCustom;
            }

            throw new NotSupportedException($"不支持的类型：{type.Name}");
        }

        // 补全：SkipObject（支持所有枚举声明的类型+List跳过）
        private static void SkipObject(BinaryReader reader)
        {
            FieldType fieldType = (FieldType)reader.ReadByte();
            switch (fieldType)
            {
                // 基础类型跳过（补全所有枚举声明的类型）
                case FieldType.Int32: reader.ReadInt32(); break;
                case FieldType.Int64: reader.ReadInt64(); break;
                case FieldType.Float: reader.ReadSingle(); break;
                case FieldType.Double: reader.ReadDouble(); break;
                case FieldType.String: reader.ReadString(); break;
                case FieldType.Bool: reader.ReadBoolean(); break;
              

                // List 类型跳过（补全所有枚举声明的List类型）
                case FieldType.ListInt32: SkipList(reader, FieldType.Int32); break;
                case FieldType.ListString: SkipList(reader, FieldType.String); break;
               
                case FieldType.ListCustom: SkipCustomList(reader); break;

                // 自定义类跳过
                case FieldType.CustomClass: SkipCustomClassWithBase(reader); break;
            }
        }

        private static void SkipCustomClassWithBase(BinaryReader reader)
        {
            int fieldCount = reader.ReadInt32();
            for (int i = 0; i < fieldCount; i++)
            {
                ReadVarint(reader); // 跳过字段ID
                SkipObject(reader); // 跳过字段值
            }
        }

        // 补全：SkipList（支持所有基础类型List跳过）
        private static void SkipList(BinaryReader reader, FieldType elementType)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                reader.ReadByte(); // 跳过元素类型标识
                SkipFieldValue(reader, elementType);
            }
        }

        private static void SkipCustomList(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++) SkipObject(reader);
        }

        // 补全：SkipFieldValue（支持所有基础类型跳过）
        private static void SkipFieldValue(BinaryReader reader, FieldType fieldType)
        {
            switch (fieldType)
            {
                case FieldType.Int32: reader.ReadInt32(); break;
                case FieldType.Int64: reader.ReadInt64(); break;
                case FieldType.Float: reader.ReadSingle(); break;
                case FieldType.Double: reader.ReadDouble(); break;
                case FieldType.Bool: reader.ReadBoolean(); break;
                case FieldType.String: reader.ReadString(); break;
               
            }
        }

        // List 序列化/反序列化（通用，无需修改）
        private static void SerializeList<T>(BinaryWriter writer, List<T> list)
        {
            writer.Write(list.Count);
            foreach (var item in list) SerializeObject(writer, item);
        }

        private static List<T> DeserializeList<T>(BinaryReader reader)
        {
            List<T> list = new List<T>();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++) list.Add((T)DeserializeObject(reader, typeof(T)));
            return list;
        }

        private static void SerializeCustomList(BinaryWriter writer, IList list)
        {
            writer.Write(list.Count);
            Type elementType = list.GetType().GetGenericArguments()[0];
            foreach (var item in list) SerializeObject(writer, item ?? Activator.CreateInstance(elementType));
        }

        private static IList DeserializeCustomList(BinaryReader reader, Type targetType)
        {
            Type elementType = targetType.GetGenericArguments()[0];
            IList list = (IList)Activator.CreateInstance(targetType);
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++) list.Add(DeserializeObject(reader, elementType));
            return list;
        }

        // Varint 编码/解码（无需修改）
        private static void WriteVarint(BinaryWriter writer, int value)
        {
            do
            {
                byte b = (byte)(value & 0x7F);
                value >>= 7;
                if (value > 0) b |= 0x80;
                writer.Write(b);
            } while (value > 0);
        }

        private static int ReadVarint(BinaryReader reader)
        {
            int value = 0;
            int shift = 0;
            byte b;
            do { b = reader.ReadByte(); value |= (b & 0x7F) << shift; shift += 7; }
            while ((b & 0x80) != 0);
            return value;
        }


        // 自定义特性（不变）
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
        public class SerializerDataFieldAttribute : Attribute
        {
            public int FieldId { get; }
            public object DefaultValue { get; set; }

            public SerializerDataFieldAttribute(int fieldId)
            {
                FieldId = fieldId;
                DefaultValue = null;
            }
        }
    }
}
