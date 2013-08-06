using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Security.RuleElements
{
    public class UserRuleElement : IRuleElement
    {
        Regex user;

        public UserRuleElement(string user)
        {
            this.user = new Regex(user, RegexOptions.IgnoreCase); ;
        }

        public bool Match(string expression)
        {
            return user.Match(expression).Success;
        }
    }
}
