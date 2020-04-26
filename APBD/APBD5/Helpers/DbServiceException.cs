using System;

namespace APBD5.Helpers
{
    public class DbServiceException : Exception
    {
        public DbServiceExceptionTypeEnum Type { get; set; }
        public DbServiceException(DbServiceExceptionTypeEnum type, string msg) : base(message: msg)
        {
            this.Type = type;
        }
    }

    public enum DbServiceExceptionTypeEnum
    {
        ValueNotUnique = 0,
        NotFound = 1,
        ProcedureError = 2
    }
}
