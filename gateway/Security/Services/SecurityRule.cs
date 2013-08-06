using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Security.RuleElements;

namespace Security.Services
{
    public class SecurityRule
    {
        public IRuleElement subject;
        public IRuleElement target;
        public string permission;
    }
}
