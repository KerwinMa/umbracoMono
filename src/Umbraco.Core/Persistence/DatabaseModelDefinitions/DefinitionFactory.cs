﻿using System;
using System.Data;
using System.Linq;
using System.Reflection;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Umbraco.Core.Persistence.DatabaseModelDefinitions
{
    internal static class DefinitionFactory
    {
        public static TableDefinition GetTableDefinition(Type modelType)
        {
            var tableNameAttribute = modelType.FirstAttribute<TableNameAttribute>();
            string tableName = tableNameAttribute.Value;

            var tableDefinition = new TableDefinition {Name = tableName};
            var objProperties = modelType.GetProperties().ToList();
            foreach (var propertyInfo in objProperties)
            {
                //If current property has an IgnoreAttribute then skip it
                var ignoreAttribute = propertyInfo.FirstAttribute<IgnoreAttribute>();
                if (ignoreAttribute != null) continue;

                //If current property has a ResultColumnAttribute then skip it
                var resultColumnAttribute = propertyInfo.FirstAttribute<ResultColumnAttribute>();
                if (resultColumnAttribute != null) continue;

                //Looks for ColumnAttribute with the name of the column, which would exist with ExplicitColumns
                //Otherwise use the name of the property itself as the default convention
                var columnAttribute = propertyInfo.FirstAttribute<ColumnAttribute>();
                string columnName = columnAttribute != null ? columnAttribute.Name : propertyInfo.Name;
                var columnDefinition = GetColumnDefinition(modelType, propertyInfo, columnName, tableName);
                tableDefinition.Columns.Add(columnDefinition);

                //Creates a foreignkey definition and adds it to the collection on the table definition
                var foreignKeyAttributes = propertyInfo.MultipleAttribute<ForeignKeyAttribute>();
                if (foreignKeyAttributes != null)
                {
                    foreach (var foreignKeyAttribute in foreignKeyAttributes)
                    {
                        var foreignKeyDefinition = GetForeignKeyDefinition(modelType, propertyInfo, foreignKeyAttribute, columnName, tableName);
                        tableDefinition.ForeignKeys.Add(foreignKeyDefinition);
                    }
                }

                //Creates an index definition and adds it to the collection on the table definition
                 var indexAttribute = propertyInfo.FirstAttribute<IndexAttribute>();
                 if (indexAttribute != null)
                 {
                     var indexDefinition = GetIndexDefinition(modelType, propertyInfo, indexAttribute, columnName, tableName);
                     tableDefinition.Indexes.Add(indexDefinition);
                 }
            }

            return tableDefinition;
        }

        public static ColumnDefinition GetColumnDefinition(Type modelType, PropertyInfo propertyInfo, string columnName, string tableName)
        {
            var definition = new ColumnDefinition{ Name = columnName, TableName = tableName, ModificationType = ModificationType.Create };

            //Look for specific Null setting attributed a column
            var nullSettingAttribute = propertyInfo.FirstAttribute<NullSettingAttribute>();
            if (nullSettingAttribute != null)
            {
                definition.IsNullable = nullSettingAttribute.NullSetting == NullSettings.Null;
            }

            //Look for specific DbType attributed a column
            var databaseTypeAttribute = propertyInfo.FirstAttribute<SpecialDbTypeAttribute>();
            if (databaseTypeAttribute != null)
            {
                definition.HasSpecialDbType = true;
                definition.DbType = databaseTypeAttribute.DatabaseType;
            }
            else
            {
                definition.PropertyType = propertyInfo.PropertyType;
            }

            //Look for Primary Key for the current column
            var primaryKeyColumnAttribute = propertyInfo.FirstAttribute<PrimaryKeyColumnAttribute>();
            if (primaryKeyColumnAttribute != null)
            {
                definition.IsPrimaryKey = true;
                definition.IsIdentity = primaryKeyColumnAttribute.AutoIncrement;
                definition.IsIndexed = primaryKeyColumnAttribute.Clustered;
                definition.PrimaryKeyName = primaryKeyColumnAttribute.Name ?? string.Empty;
                definition.PrimaryKeyColumns = primaryKeyColumnAttribute.OnColumns ?? string.Empty;
                definition.Seeding = primaryKeyColumnAttribute.IdentitySeed;
            }

            //Look for Size/Length of DbType
            var lengthAttribute = propertyInfo.FirstAttribute<LengthAttribute>();
            if (lengthAttribute != null)
            {
                definition.Size = lengthAttribute.Length;
            }

            //Look for Constraint for the current column
            var constraintAttribute = propertyInfo.FirstAttribute<ConstraintAttribute>();
            if (constraintAttribute != null)
            {
                definition.ConstraintName = constraintAttribute.Name ?? string.Empty;
                definition.DefaultValue = constraintAttribute.Default ?? string.Empty;
            }

            return definition;
        }

        public static ForeignKeyDefinition GetForeignKeyDefinition(Type modelType, PropertyInfo propertyInfo,
                                                                   ForeignKeyAttribute attribute, string columnName, string tableName)
        {
            var referencedTable = attribute.Type.FirstAttribute<TableNameAttribute>();
            var referencedPrimaryKey = attribute.Type.FirstAttribute<PrimaryKeyAttribute>();

            string referencedColumn = string.IsNullOrEmpty(attribute.Column)
                                          ? referencedPrimaryKey.Value
                                          : attribute.Column;

            var definition = new ForeignKeyDefinition
                                 {
                                     Name = attribute.Name,
                                     ForeignTable = tableName,
                                     PrimaryTable = referencedTable.Value
                                 };
            definition.ForeignColumns.Add(columnName);
            definition.PrimaryColumns.Add(referencedColumn);

            return definition;
        }

        public static IndexDefinition GetIndexDefinition(Type modelType, PropertyInfo propertyInfo, IndexAttribute attribute, string columnName, string tableName)
        {
            var definition = new IndexDefinition
                                 {
                                     Name = attribute.Name,
                                     IndexType = attribute.IndexType,
                                     ColumnName = columnName,
                                     TableName = tableName,
                                     IsClustered = attribute.IndexType == IndexTypes.Clustered,
                                     IsUnique = attribute.IndexType == IndexTypes.UniqueNonClustered
                                 };

            if (string.IsNullOrEmpty(attribute.ForColumns) == false)
            {
                var columns = attribute.ForColumns.Split(',').Select(p => p.Trim());
                foreach (var column in columns)
                {
                    definition.Columns.Add(new IndexColumnDefinition {Name = column, Direction = Direction.Ascending});
                }
            }
            return definition;
        }
    }
}