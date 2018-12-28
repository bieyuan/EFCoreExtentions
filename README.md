# EFCoreExtentions 
 - EFCore扩展Select方法(根据实体定制查询语句) 
 - Extentions Update Method 支持 UPDATE User SET Id=Id+1 自更新操作 
 
## Select 调用方法

### 构造测试类
```C#
    public partial class User
    {
        public int Id { get; set; }
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        public int RoleId { get; set; }

        [ForeignKey(nameof(RoleId))]
        public virtual Role Role { get; set; }
    }
    
    public partial class Role
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int DepartmentId { get; set; }

        [ForeignKey(nameof(DepartmentId))]
        public virtual Department Department  { get; set; } 
    }

    public partial class Department
    {
        public int Id { get; set; }
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
    }
 ```
    
### 构造查询结果模型
``` C#
    public partial class UserModel
    {
        public string Name { get; set; }

        public string RoleName { get; set; }

        //[FromEntity("Name","Role")]
        //public string RoleName1 { get; set; }

        [FromEntity("Name", "Department", "Role")]
        public string DepartmentName { get; set; }

    }
```
    
 ### 查询代码如下
 
``` C#
static void Main(string[] args)
        {
            using (var context = new TestContext())
            {
                var list = context.User.Select<UserModel>().ToList();
            }
            Console.WriteLine($"------------结束--------------------");
            Console.ReadLine();
        }
```
            
### 生成的sql语句 如下图
``` SQL
 SELECT [u].[Name] AS [Name0], [u.Role].[Name] AS [RoleName], [u.Role.Department].[Name] AS [DepartmentName]
      FROM [User] AS [u]
      INNER JOIN [Role] AS [u.Role] ON [u].[RoleId] = [u.Role].[Id]
      INNER JOIN [Department] AS [u.Role.Department] ON [u.Role].[DepartmentId] = [u.Role.Department].[Id]
 ```
此方案用在接口，精确查询字段，需要强类型视图的地方相对比较方便



## 扩展Update方法
```C#
 static void Main(string[] args)
        {
            using (var context = new TestContext())
            {
                var list = context.User.Select<UserModel>().ToList();

                var user1 = context.User.AsNoTracking().FirstOrDefault(x=>x.Id==2);

                Console.WriteLine($"-----------Before Update --------------------");
                Console.WriteLine($"{user1.Id}:{user1.Name}:{user1.RoleId}");

                context.User.Where(x => x.Id == 2).RestValue(x => x.Name == (x.Name + " Add Bob") && x.RoleId == (x.RoleId + 1));

                var user2 = context.User.AsNoTracking().FirstOrDefault(x => x.Id == 2);

                Console.WriteLine($"-----------After Update --------------------");
                Console.WriteLine($"{user2.Id}:{user2.Name}:{user2.RoleId}");
            }
            Console.WriteLine($"------------结束--------------------");
            Console.ReadLine();
        }
 ```
 生成的SQL如下
 
 ```SQL
       UPDATE [x] SET x.[Name] =x.[Name] + @param_0 ,x.[RoleId] =x.[RoleId] + @param_1
      FROM [User] AS [x]
      WHERE [x].[Id] = 2
 ```
        

 



