using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Security
{
    public class Permission : IPermission
    {
        bool hasReadPermission = false;
        bool hasWritePermission = false;

        public void AddWritePermission()
        {
            hasWritePermission = true;
        }

        public void RemoveWritePermission()
        {
            hasWritePermission = false;
        }

        public void AddReadPermission()
        {
            hasReadPermission = true;
        }

        public void RemoveReadPermission()
        {
            hasReadPermission = false;
        }

        public bool HasReadPermission()
        {
            return hasReadPermission;
        }

        public bool HasWritePermission()
        {
            return hasWritePermission;
        }
    }
}
