using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Security.RuleElements
{
    public interface IRuleElement
    {
        bool Match(string expression);
    }
}
