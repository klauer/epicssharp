using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Security.RuleElements
{
    public class ChannelRuleElement : IRuleElement
    {
        
        Regex channel;

        public ChannelRuleElement(string channel)
        {
            this.channel = new Regex(channel, RegexOptions.None);
        }

        public bool Match(string expression)
        {
            return channel.Match(expression).Success;
        }
    }
}
