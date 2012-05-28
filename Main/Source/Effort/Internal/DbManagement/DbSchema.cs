﻿#region License

// Copyright (c) 2011 Effort Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Effort.Internal.DbManagement
{
    internal class DbSchema
    {
        private Dictionary<string, DbTableInformation> tables;
        private List<DbRelationInformation> relations;


        public DbSchema()
        {
            this.tables = new Dictionary<string, DbTableInformation>();
            this.relations = new List<DbRelationInformation>();
        }

        public void RegisterTable(string tableName, Type entityType, PropertyInfo[] primaryKeyFields, PropertyInfo identityField,
            PropertyInfo[] properties)
        {
            DbTableInformation table = new DbTableInformation(tableName, entityType, primaryKeyFields, identityField, properties);

            this.tables.Add(tableName, table);
        }

        public void RegisterRelation(string fromTable, PropertyInfo[] fromProperties, string toTable, PropertyInfo[] toProperties)
        {
            DbRelationInformation relation = new DbRelationInformation(fromTable, fromProperties, toTable, toProperties);

            this.relations.Add(relation);
        }

        public DbTableInformation GetTable(string tableName)
        {
            return this.tables[tableName];
        }

        public string[] GetTableNames()
        {
            return this.tables.Keys.ToArray();
        }

        public DbTableInformation[] Tables 
        {
            get { return this.tables.Values.ToArray(); }
        }

        public DbRelationInformation[] Relations
        {
            get { return this.relations.ToArray(); }
        }

    }
}