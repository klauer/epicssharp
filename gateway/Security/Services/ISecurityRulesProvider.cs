using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Security.Services
{
    public interface ISecurityRulesProvider
    {
        List<SecurityRule> GetAll();
    }
}
