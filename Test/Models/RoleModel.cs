using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Test
{
    public partial class RoleModel
    {
        public string Name { get; set; }
        public string DepartmentName { get; set; }

        public virtual DepartmentModel Department  { get; set; } 
    }
}
