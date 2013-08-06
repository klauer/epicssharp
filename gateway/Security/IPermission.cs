using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Security
{
    public interface IPermission
    {
        void AddWritePermission();
        void RemoveWritePermission();
        void AddReadPermission();
        void RemoveReadPermission();
        bool HasReadPermission();
        bool HasWritePermission();
    }
}
