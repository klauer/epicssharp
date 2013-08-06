using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Security.Services
{
    public class SecurityService
    {
        public ISecurityRulesProvider securityRulesProvider;

        public IPermission GetPermission(string user, string channel)
        {
            IPermission result = new Permission();

            List<SecurityRule> rules = securityRulesProvider.GetAll();

            foreach (SecurityRule rule in rules)
            {
                if (rule.subject.Match(user) && rule.target.Match(channel))
                {
                    switch (rule.permission.ToUpper())
                    {
                        case "R":
                            result.AddReadPermission();
                            break;
                        case "W":
                            result.AddWritePermission();
                            break;
                        case "RW":
                            result.AddReadPermission();
                            result.AddWritePermission();
                            break;
                        case "-R":
                            result.RemoveReadPermission();
                            break;
                        case "-W":
                            result.RemoveWritePermission();
                            break;
                        case "-RW":
                            result.RemoveWritePermission();
                            result.RemoveReadPermission();
                            break;
                    }
                }
            }

            return result;
        }
    }
}
