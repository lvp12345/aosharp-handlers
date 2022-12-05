using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LootManager
{
    public class Rule
    {

        public string Name = "";
        public string Lql = "";
        public string Hql = "";
        public bool Global = true;

        public Rule(string Name, string Lql, string Hql, bool Global)
        {
            this.Name = Name;
            this.Lql = Lql;
            this.Hql = Hql;
            this.Global = Global;
        }

    }
}
