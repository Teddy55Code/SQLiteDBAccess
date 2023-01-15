using System;
using System.Reflection;
using MethodDecorator.Fody.Interfaces;

namespace SQLiteDBAccess.Decorators
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Assembly | AttributeTargets.Module)]
    internal class ManageFileAttribute : Attribute, IMethodDecorator
    {
        public bool IsConnectionPreserved = false;
        
        private SQLiteDBAccess access;
        
        public void Init(object instance, MethodBase method, object[] args)
        {
            access = (SQLiteDBAccess)instance;
        }

        public void OnEntry()
        {
            if (access.IsFileManaged) access.OpenDBFile();
        }

        public void OnExit() 
        {
            if (access.IsFileManaged && !IsConnectionPreserved) access.CloseDBFile();
        }

        public void OnException(Exception exception) {
        }
    }
}