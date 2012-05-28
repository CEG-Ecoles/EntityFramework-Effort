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
using System.ComponentModel;
using System.Data.EntityClient;
using System.Data.Objects;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Effort.Internal.Caching;
using Effort.Internal.Common;
using Effort.Provider;
using Effort.DataProviders;

namespace Effort
{
    public static class ObjectContextFactory
    {
        private static ModuleBuilder objectContextContainer;
        private static int objectContextCounter;

        static ObjectContextFactory()
        {
            // Dynamic Library for Effort
            AssemblyBuilder assembly =
                Thread.GetDomain().DefineDynamicAssembly(
                    new AssemblyName(string.Format("DynamicObjectContextLib")),
                    AssemblyBuilderAccess.Run);

            // Module for the entity types
            objectContextContainer = assembly.DefineDynamicModule("ObjectContexts");
            objectContextCounter = 0;
        }

        #region Persistent

        public static Type CreatePersistentType<T>(string entityConnectionString, IDataProvider dataProvider) 
            where T : ObjectContext
        {
            return CreateType<T>(entityConnectionString, true, dataProvider);
        }

        public static Type CreatePersistentType<T>(string entityConnectionString) where T : ObjectContext
        {
            return CreateType<T>(entityConnectionString, true, null);
        }

        public static Type CreatePersistentType<T>() where T : ObjectContext
        {
            return CreateType<T>(null, true, null);
        }

        public static Type CreatePersistentType<T>(IDataProvider dataProvider)
            where T : ObjectContext
        {
            return CreateType<T>(null, true, dataProvider);
        }

        #endregion



        #region Transient

        public static Type CreateTransientType<T>(string entityConnectionString, IDataProvider dataProvider)
            where T : ObjectContext
        {
            return CreateType<T>(entityConnectionString, false, dataProvider);
        }

        public static Type CreateTransientType<T>(string entityConnectionString) where T : ObjectContext
        {
            return CreateType<T>(entityConnectionString, false, null);
        }

        public static Type CreateTransientType<T>() where T : ObjectContext
        {
            return CreateType<T>(null, false, null);
        }

        public static Type CreateTransientType<T>(IDataProvider dataProvider)
            where T : ObjectContext
        {
            return CreateType<T>(null, false, dataProvider);
        }

        #endregion




        private static Type CreateType<T>(string entityConnectionString, bool persistent, IDataProvider dataProvider) where T : ObjectContext
        {
            EffortConnectionStringBuilder ecsb = new EffortConnectionStringBuilder();

            if (dataProvider != null)
            {
                ecsb.DataProviderType = dataProvider.GetType();
                ecsb.DataProviderArg = dataProvider.Argument;
            }

            string effortConnectionString = ecsb.ConnectionString;

            return ObjectContextTypeStore.GetObjectContextType(entityConnectionString, effortConnectionString, typeof(T), () =>
                {
                    if (string.IsNullOrEmpty(entityConnectionString))
                    {
                        entityConnectionString = GetDefaultConnectionString<T>();
                    }

                    return CreateType<T>(entityConnectionString, effortConnectionString, persistent);
                });
        }


        private static string GetDefaultConnectionString<T>() where T : ObjectContext
        {
            return Activator.CreateInstance<T>().Connection.ConnectionString;

            // TODO: Search for default connection string
        }

        private static Type CreateType<T>(string entityConnectionString, string effortConnectionString, bool persistent)
        {
            TypeBuilder builder = null;

            lock (objectContextContainer)
            {
                objectContextCounter++;
                builder = objectContextContainer.DefineType(
                    string.Format("DynamicObjectContext{0}", objectContextCounter),
                    TypeAttributes.Public,
                    typeof(T));
            }

            ConstructorBuilder ctor = builder.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.Standard, new Type[] { });

            ConstructorInfo baseCtor = typeof(T).GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new Type[] { typeof(EntityConnection) },
                null
                );


            ILGenerator gen = ctor.GetILGenerator();
            // Writing body
            gen.Emit(OpCodes.Ldarg_0);

            gen.Emit(OpCodes.Ldstr, entityConnectionString);
            gen.Emit(OpCodes.Ldstr, effortConnectionString);
            gen.Emit(persistent ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);

            MethodInfo entityConnectionFactory = ReflectionHelper.GetMethodInfo<object>(a =>
                       EntityConnectionFactory.Create(string.Empty, string.Empty, false));

            gen.Emit(OpCodes.Call, entityConnectionFactory);
            gen.Emit(OpCodes.Call, baseCtor);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Ret);


            // protected void Dispose(bool disposing)
            MethodInfo baseDispose = typeof(T).GetMethod(
                "Dispose",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new Type[] { typeof(bool) },
                null);

            // public void Dispose()
            MethodInfo connectionDispose = typeof(Component).GetMethod(
                "Dispose",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                Type.EmptyTypes,
                null);

            MethodInfo connectionGetter = typeof(T).GetProperty("Connection").GetGetMethod();

            MethodBuilder overridedDispose = builder.DefineMethod(
                "Dispose",
                MethodAttributes.Family | 
                MethodAttributes.Virtual | 
                MethodAttributes.HideBySig | 
                MethodAttributes.ReuseSlot);

            overridedDispose.SetReturnType(typeof(void));
            // Adding parameters
            overridedDispose.SetParameters(typeof(bool));

            gen = overridedDispose.GetILGenerator();
            LocalBuilder l0 = gen.DeclareLocal(typeof(bool));

            Label label = gen.DefineLabel();

            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Ldc_I4_0);
            gen.Emit(OpCodes.Ceq);
            gen.Emit(OpCodes.Stloc_0);
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Brtrue_S, label);

            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Call, connectionGetter);
            gen.Emit(OpCodes.Callvirt, connectionDispose);

            gen.MarkLabel(label);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Call, baseDispose);

            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Ret);

            return builder.CreateType();
        }
    }
}