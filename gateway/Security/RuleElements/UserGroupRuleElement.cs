using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Security.RuleElements
{
    public class UserGroupRuleElement : IRuleElement
    {
        List<UserRuleElement> users;

        public UserGroupRuleElement(List<UserRuleElement> users)
        {
            this.users = users;
        }

        public bool Match(string expression)
        {
            bool result = false;

            foreach (UserRuleElement user in users)
            {
                if (user.Match(expression))
                {
                    result = true;
                }
            }
            return result;
        }
    }
}
