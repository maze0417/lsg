using System;

namespace LSG.Core.Exceptions
{
    public class BrandUnderMaintenanceException : Exception
    {
        public BrandUnderMaintenanceException() : base("Brand is maintenance")
        {
        }
    }
}