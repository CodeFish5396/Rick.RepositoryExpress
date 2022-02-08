using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class RickDBConext : DbContext
    {
        public RickDBConext()
        {
        }

        public RickDBConext(DbContextOptions<RickDBConext> options)
            : base(options)
        {
        }

        public virtual DbSet<Account> Accounts { get; set; }
        public virtual DbSet<Accountsubject> Accountsubjects { get; set; }
        public virtual DbSet<Agent> Agents { get; set; }
        public virtual DbSet<Agentandcourier> Agentandcouriers { get; set; }
        public virtual DbSet<Agentfee> Agentfees { get; set; }
        public virtual DbSet<Agentfeeaccount> Agentfeeaccounts { get; set; }
        public virtual DbSet<Agentfeeconsume> Agentfeeconsumes { get; set; }
        public virtual DbSet<Appnew> Appnews { get; set; }
        public virtual DbSet<Appuser> Appusers { get; set; }
        public virtual DbSet<Appuseraccount> Appuseraccounts { get; set; }
        public virtual DbSet<Appuseraccountcharge> Appuseraccountcharges { get; set; }
        public virtual DbSet<Appuseraccountchargeimage> Appuseraccountchargeimages { get; set; }
        public virtual DbSet<Appuseraccountconsume> Appuseraccountconsumes { get; set; }
        public virtual DbSet<Appuseraddress> Appuseraddresses { get; set; }
        public virtual DbSet<Channel> Channels { get; set; }
        public virtual DbSet<Channeldescription> Channeldescriptions { get; set; }
        public virtual DbSet<Channeldetail> Channeldetails { get; set; }
        public virtual DbSet<Channellimit> Channellimits { get; set; }
        public virtual DbSet<Channelprice> Channelprices { get; set; }
        public virtual DbSet<Channeltype> Channeltypes { get; set; }
        public virtual DbSet<Courier> Couriers { get; set; }
        public virtual DbSet<Currency> Currencies { get; set; }
        public virtual DbSet<Currencychangerate> Currencychangerates { get; set; }
        public virtual DbSet<Expressclaim> Expressclaims { get; set; }
        public virtual DbSet<Expressclaimdetail> Expressclaimdetails { get; set; }
        public virtual DbSet<Expressinfo> Expressinfos { get; set; }
        public virtual DbSet<Expressinfostatus> Expressinfostatuses { get; set; }
        public virtual DbSet<Fileinfo> Fileinfos { get; set; }
        public virtual DbSet<Message> Messages { get; set; }
        public virtual DbSet<Messageconsume> Messageconsumes { get; set; }
        public virtual DbSet<Nation> Nations { get; set; }
        public virtual DbSet<Package> Packages { get; set; }
        public virtual DbSet<Packagedetail> Packagedetails { get; set; }
        public virtual DbSet<Packageexchangeapply> Packageexchangeapplies { get; set; }
        public virtual DbSet<Packageimage> Packageimages { get; set; }
        public virtual DbSet<Packagenote> Packagenotes { get; set; }
        public virtual DbSet<Packageorderapply> Packageorderapplies { get; set; }
        public virtual DbSet<Packageorderapplydetail> Packageorderapplydetails { get; set; }
        public virtual DbSet<Packageorderapplyerror> Packageorderapplyerrors { get; set; }
        public virtual DbSet<Packageorderapplyerrorlog> Packageorderapplyerrorlogs { get; set; }
        public virtual DbSet<Packageorderapplyexpress> Packageorderapplyexpresses { get; set; }
        public virtual DbSet<Packageorderapplyexpressdetail> Packageorderapplyexpressdetails { get; set; }
        public virtual DbSet<Packageorderapplyexpresspackage> Packageorderapplyexpresspackages { get; set; }
        public virtual DbSet<Packageorderapplyexpressstatus> Packageorderapplyexpressstatuses { get; set; }
        public virtual DbSet<Packageorderapplynote> Packageorderapplynotes { get; set; }
        public virtual DbSet<Packagerefundapply> Packagerefundapplies { get; set; }
        public virtual DbSet<Packagevideo> Packagevideos { get; set; }
        public virtual DbSet<Refundorder> Refundorders { get; set; }
        public virtual DbSet<Repository> Repositories { get; set; }
        public virtual DbSet<Repositorylayer> Repositorylayers { get; set; }
        public virtual DbSet<Repositoryregion> Repositoryregions { get; set; }
        public virtual DbSet<Repositoryshelf> Repositoryshelves { get; set; }
        public virtual DbSet<Runfee> Runfees { get; set; }
        public virtual DbSet<Syscompany> Syscompanies { get; set; }
        public virtual DbSet<Sysdepartment> Sysdepartments { get; set; }
        public virtual DbSet<Sysfunction> Sysfunctions { get; set; }
        public virtual DbSet<Sysmenu> Sysmenus { get; set; }
        public virtual DbSet<Sysrole> Sysroles { get; set; }
        public virtual DbSet<Sysrolemenufunction> Sysrolemenufunctions { get; set; }
        public virtual DbSet<Syssetting> Syssettings { get; set; }
        public virtual DbSet<Sysuser> Sysusers { get; set; }
        public virtual DbSet<Sysusercompany> Sysusercompanies { get; set; }
        public virtual DbSet<Sysuserdepartment> Sysuserdepartments { get; set; }
        public virtual DbSet<Sysuserrole> Sysuserroles { get; set; }
        public virtual DbSet<Viewagentuserdatum> Viewagentuserdata { get; set; }
        public virtual DbSet<Viewappuserdatum> Viewappuserdata { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseMySql("server=192.168.15.42;port=3306;user=root;password=Sunwin@2021;database=repositoryexpress", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.21-mysql"));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasCharSet("utf8mb4")
                .UseCollation("utf8mb4_0900_ai_ci");

            modelBuilder.Entity<Account>(entity =>
            {
                entity.ToTable("account");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Amount)
                    .HasPrecision(10)
                    .HasColumnName("amount");

                entity.Property(e => e.Currencyid).HasColumnName("currencyid");

                entity.Property(e => e.Direction)
                    .HasColumnName("direction")
                    .HasComment("-1 减少 +1 增加");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Subjectcode)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("subjectcode");
            });

            modelBuilder.Entity<Accountsubject>(entity =>
            {
                entity.ToTable("accountsubject");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("code");

                entity.Property(e => e.Direction)
                    .HasColumnName("direction")
                    .HasComment("-1  支付项目  1 收入项目");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("name");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Agent>(entity =>
            {
                entity.ToTable("agent");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Address)
                    .HasMaxLength(200)
                    .HasColumnName("address");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Contact)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("contact");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Mobile)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("mobile");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("name");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Agentandcourier>(entity =>
            {
                entity.ToTable("agentandcourier");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Agentid).HasColumnName("agentid");

                entity.Property(e => e.Courierid).HasColumnName("courierid");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Agentfee>(entity =>
            {
                entity.ToTable("agentfee");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Accountid).HasColumnName("accountid");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Agentid).HasColumnName("agentid");

                entity.Property(e => e.Paytype)
                    .HasColumnName("paytype")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Agentfeeaccount>(entity =>
            {
                entity.ToTable("agentfeeaccount");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Agentid).HasColumnName("agentid");

                entity.Property(e => e.Amount)
                    .HasPrecision(10, 2)
                    .HasColumnName("amount");

                entity.Property(e => e.Currencyid).HasColumnName("currencyid");

                entity.Property(e => e.Status).HasColumnName("status");
            });

            modelBuilder.Entity<Agentfeeconsume>(entity =>
            {
                entity.ToTable("agentfeeconsume");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Agentid).HasColumnName("agentid");

                entity.Property(e => e.Amount)
                    .HasPrecision(10, 2)
                    .HasColumnName("amount");

                entity.Property(e => e.Currencyid).HasColumnName("currencyid");

                entity.Property(e => e.Orderid).HasColumnName("orderid");

                entity.Property(e => e.Status).HasColumnName("status");
            });

            modelBuilder.Entity<Appnew>(entity =>
            {
                entity.ToTable("appnew");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Imageid).HasColumnName("imageid");

                entity.Property(e => e.Isshow).HasColumnName("isshow");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Status).HasColumnName("status");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnName("title");

                entity.Property(e => e.Type).HasColumnName("type");

                entity.Property(e => e.Urlid).HasColumnName("urlid");

                entity.Property(e => e.Vicetitle)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnName("vicetitle");
            });

            modelBuilder.Entity<Appuser>(entity =>
            {
                entity.ToTable("appuser");

                entity.HasIndex(e => e.Openid, "openid_status_UNIQUE")
                    .IsUnique();

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.AddUser).HasColumnName("addUser");

                entity.Property(e => e.Address)
                    .HasMaxLength(200)
                    .HasColumnName("address");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasComment("注册时间");

                entity.Property(e => e.Birthdate)
                    .HasColumnType("date")
                    .HasColumnName("birthdate");

                entity.Property(e => e.Cityname)
                    .HasMaxLength(45)
                    .HasColumnName("cityname");

                entity.Property(e => e.Countrycode)
                    .HasMaxLength(45)
                    .HasColumnName("countrycode");

                entity.Property(e => e.Countryname)
                    .HasMaxLength(45)
                    .HasColumnName("countryname");

                entity.Property(e => e.Email)
                    .HasMaxLength(45)
                    .HasColumnName("email");

                entity.Property(e => e.Gender)
                    .HasMaxLength(45)
                    .HasColumnName("gender");

                entity.Property(e => e.Headportrait)
                    .HasMaxLength(45)
                    .HasColumnName("headportrait")
                    .HasComment("头像");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime");

                entity.Property(e => e.Mobile)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("mobile");

                entity.Property(e => e.Name)
                    .HasMaxLength(45)
                    .HasColumnName("name");

                entity.Property(e => e.Openid)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("openid")
                    .HasComment("微信用户openid");

                entity.Property(e => e.Shareuser).HasColumnName("shareuser");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'")
                    .HasComment("用户状态 0:无效 1:正常");

                entity.Property(e => e.Truename)
                    .HasMaxLength(45)
                    .HasColumnName("truename");

                entity.Property(e => e.Usercode)
                    .HasMaxLength(45)
                    .HasColumnName("usercode");
            });

            modelBuilder.Entity<Appuseraccount>(entity =>
            {
                entity.ToTable("appuseraccount");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Amount)
                    .HasPrecision(10, 2)
                    .HasColumnName("amount");

                entity.Property(e => e.Appuser).HasColumnName("appuser");

                entity.Property(e => e.Currencyid).HasColumnName("currencyid");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Appuseraccountcharge>(entity =>
            {
                entity.ToTable("appuseraccountcharge");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Accountid).HasColumnName("accountid");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Amount)
                    .HasPrecision(10, 2)
                    .HasColumnName("amount");

                entity.Property(e => e.Appuser).HasColumnName("appuser");

                entity.Property(e => e.Currencyid).HasColumnName("currencyid");

                entity.Property(e => e.Paytype)
                    .HasColumnName("paytype")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Remark)
                    .HasMaxLength(500)
                    .HasColumnName("remark");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Appuseraccountchargeimage>(entity =>
            {
                entity.ToTable("appuseraccountchargeimages");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Appuseraccountchargeid).HasColumnName("appuseraccountchargeid");

                entity.Property(e => e.Fileinfoid).HasColumnName("fileinfoid");

                entity.Property(e => e.Status).HasColumnName("status");
            });

            modelBuilder.Entity<Appuseraccountconsume>(entity =>
            {
                entity.ToTable("appuseraccountconsume");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Amount)
                    .HasPrecision(10, 2)
                    .HasColumnName("amount");

                entity.Property(e => e.Appuser).HasColumnName("appuser");

                entity.Property(e => e.Curencyid).HasColumnName("curencyid");

                entity.Property(e => e.Orderid).HasColumnName("orderid");

                entity.Property(e => e.Remark)
                    .HasMaxLength(500)
                    .HasColumnName("remark");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Appuseraddress>(entity =>
            {
                entity.ToTable("appuseraddress");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Address)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnName("address");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Appuser).HasColumnName("appuser");

                entity.Property(e => e.Contactnumber)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("contactnumber");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("name");

                entity.Property(e => e.Nationid).HasColumnName("nationid");

                entity.Property(e => e.Region)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnName("region");

                entity.Property(e => e.Status).HasColumnName("status");

                entity.Property(e => e.Weight).HasColumnName("weight");
            });

            modelBuilder.Entity<Channel>(entity =>
            {
                entity.ToTable("channel");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("name");

                entity.Property(e => e.Order)
                    .HasColumnName("order")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Unitprice)
                    .HasPrecision(10, 2)
                    .HasColumnName("unitprice")
                    .HasDefaultValueSql("'1.00'");
            });

            modelBuilder.Entity<Channeldescription>(entity =>
            {
                entity.ToTable("channeldescription");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Channelid).HasColumnName("channelid");

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnName("description");

                entity.Property(e => e.Order).HasColumnName("order");
            });

            modelBuilder.Entity<Channeldetail>(entity =>
            {
                entity.ToTable("channeldetails");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Agentid).HasColumnName("agentid");

                entity.Property(e => e.Channelid).HasColumnName("channelid");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Nationid).HasColumnName("nationid");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Channellimit>(entity =>
            {
                entity.ToTable("channellimit");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Channelid).HasColumnName("channelid");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnName("name");

                entity.Property(e => e.Order).HasColumnName("order");

                entity.Property(e => e.Value)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnName("value");
            });

            modelBuilder.Entity<Channelprice>(entity =>
            {
                entity.ToTable("channelprice");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Channelid).HasColumnName("channelid");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Maxweight)
                    .HasPrecision(10, 2)
                    .HasColumnName("maxweight");

                entity.Property(e => e.Minweight)
                    .HasPrecision(10, 2)
                    .HasColumnName("minweight");

                entity.Property(e => e.Nationid).HasColumnName("nationid");

                entity.Property(e => e.Price)
                    .HasPrecision(10, 2)
                    .HasColumnName("price");

                entity.Property(e => e.Status).HasColumnName("status");
            });

            modelBuilder.Entity<Channeltype>(entity =>
            {
                entity.ToTable("channeltype");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Channelid).HasColumnName("channelid");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("name");
            });

            modelBuilder.Entity<Courier>(entity =>
            {
                entity.ToTable("courier");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("code");

                entity.Property(e => e.Extname)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("extname");

                entity.Property(e => e.Hasoutdoor).HasColumnName("hasoutdoor");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("name");

                entity.Property(e => e.Order)
                    .HasColumnName("order")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Type).HasColumnName("type");
            });

            modelBuilder.Entity<Currency>(entity =>
            {
                entity.ToTable("currency");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("code");

                entity.Property(e => e.Isdefault).HasColumnName("isdefault");

                entity.Property(e => e.Islocal).HasColumnName("islocal");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("name");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Currencychangerate>(entity =>
            {
                entity.ToTable("currencychangerate");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Rate)
                    .HasPrecision(10, 6)
                    .HasColumnName("rate");

                entity.Property(e => e.Sourcecurrency).HasColumnName("sourcecurrency");

                entity.Property(e => e.Status).HasColumnName("status");

                entity.Property(e => e.Targetcurrency).HasColumnName("targetcurrency");
            });

            modelBuilder.Entity<Expressclaim>(entity =>
            {
                entity.ToTable("expressclaim");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Appuser).HasColumnName("appuser");

                entity.Property(e => e.Cansendasap)
                    .HasColumnName("cansendasap")
                    .HasComment("0:false 1 true");

                entity.Property(e => e.Count)
                    .HasColumnName("count")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Expressinfoid).HasColumnName("expressinfoid");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Packageid).HasColumnName("packageid");

                entity.Property(e => e.Remark)
                    .HasMaxLength(200)
                    .HasColumnName("remark");

                entity.Property(e => e.Repositoryid).HasColumnName("repositoryid");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'")
                    .HasComment("0：已删除 1：正常 2：已申请 3：已发货");
            });

            modelBuilder.Entity<Expressclaimdetail>(entity =>
            {
                entity.ToTable("expressclaimdetails");

                entity.HasIndex(e => e.Expressclaimid, "IX_preexpressorderid");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Count).HasColumnName("count");

                entity.Property(e => e.Expressclaimid).HasColumnName("expressclaimid");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("name");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Unitprice)
                    .HasPrecision(10)
                    .HasColumnName("unitprice");
            });

            modelBuilder.Entity<Expressinfo>(entity =>
            {
                entity.ToTable("expressinfo");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Courierid).HasColumnName("courierid");

                entity.Property(e => e.Currentdetails)
                    .HasMaxLength(500)
                    .HasColumnName("currentdetails");

                entity.Property(e => e.Expressnumber)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("expressnumber");

                entity.Property(e => e.Expressstatus)
                    .HasColumnName("expressstatus")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Lastdetails)
                    .HasMaxLength(500)
                    .HasColumnName("lastdetails");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Recivermobil)
                    .HasMaxLength(45)
                    .HasColumnName("recivermobil");

                entity.Property(e => e.Recivername)
                    .HasMaxLength(45)
                    .HasColumnName("recivername");

                entity.Property(e => e.Reciversddress)
                    .HasMaxLength(45)
                    .HasColumnName("reciversddress");

                entity.Property(e => e.Senderaddress)
                    .HasMaxLength(45)
                    .HasColumnName("senderaddress");

                entity.Property(e => e.Sendermobil)
                    .HasMaxLength(45)
                    .HasColumnName("sendermobil");

                entity.Property(e => e.Sendername)
                    .HasMaxLength(45)
                    .HasColumnName("sendername");

                entity.Property(e => e.Source)
                    .HasColumnName("source")
                    .HasComment("1：APP用户申请 2：系统用户入库");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Expressinfostatus>(entity =>
            {
                entity.ToTable("expressinfostatus");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime).HasColumnType("datetime");

                entity.Property(e => e.Expressinfoid).HasColumnName("expressinfoid");

                entity.Property(e => e.Location)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnName("location");

                entity.Property(e => e.Searchtime)
                    .HasColumnType("datetime")
                    .HasColumnName("searchtime");
            });

            modelBuilder.Entity<Fileinfo>(entity =>
            {
                entity.ToTable("fileinfo");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Ext)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("ext");

                entity.Property(e => e.Filename)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("filename");

                entity.Property(e => e.Mime)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("mime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("name");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.ToTable("message");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Index)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("index");

                entity.Property(e => e.Isclosed).HasColumnName("isclosed");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Message1)
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnName("message");

                entity.Property(e => e.Sender).HasColumnName("sender");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Messageconsume>(entity =>
            {
                entity.ToTable("messageconsume");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Messageid).HasColumnName("messageid");

                entity.Property(e => e.Sysuser).HasColumnName("sysuser");
            });

            modelBuilder.Entity<Nation>(entity =>
            {
                entity.ToTable("nation");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("code");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("name");

                entity.Property(e => e.Order)
                    .HasColumnName("order")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Package>(entity =>
            {
                entity.ToTable("package");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Changecode)
                    .HasMaxLength(45)
                    .HasColumnName("changecode");

                entity.Property(e => e.Changeremark)
                    .HasMaxLength(500)
                    .HasColumnName("changeremark");

                entity.Property(e => e.Checkremark)
                    .HasMaxLength(500)
                    .HasColumnName("checkremark");

                entity.Property(e => e.Claimtype)
                    .HasColumnName("claimtype")
                    .HasComment("0 未认领 1 先预报后入库 2 先入库后认领");

                entity.Property(e => e.Code)
                    .HasMaxLength(45)
                    .HasColumnName("code");

                entity.Property(e => e.Count)
                    .HasColumnName("count")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Courierid).HasColumnName("courierid");

                entity.Property(e => e.Currencychangerate)
                    .HasPrecision(10, 6)
                    .HasColumnName("currencychangerate");

                entity.Property(e => e.Currencychangerateid).HasColumnName("currencychangerateid");

                entity.Property(e => e.Expressinfoid).HasColumnName("expressinfoid");

                entity.Property(e => e.Expressnumber)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("expressnumber");

                entity.Property(e => e.Freightprice)
                    .HasPrecision(10, 2)
                    .HasColumnName("freightprice");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Localfreightprice)
                    .HasPrecision(10, 2)
                    .HasColumnName("localfreightprice");

                entity.Property(e => e.Location)
                    .HasMaxLength(45)
                    .HasColumnName("location");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnName("name");

                entity.Property(e => e.Refundcode)
                    .HasMaxLength(45)
                    .HasColumnName("refundcode");

                entity.Property(e => e.Refundremark)
                    .HasMaxLength(500)
                    .HasColumnName("refundremark");

                entity.Property(e => e.Remark)
                    .HasMaxLength(450)
                    .HasColumnName("remark");

                entity.Property(e => e.Repositoryid).HasColumnName("repositoryid");

                entity.Property(e => e.Repositoryintime)
                    .HasColumnType("datetime")
                    .HasColumnName("repositoryintime");

                entity.Property(e => e.Repositoryinuser).HasColumnName("repositoryinuser");

                entity.Property(e => e.Repositorylayerid).HasColumnName("repositorylayerid");

                entity.Property(e => e.Repositorynumber)
                    .HasMaxLength(45)
                    .HasColumnName("repositorynumber");

                entity.Property(e => e.Repositoryregionid).HasColumnName("repositoryregionid");

                entity.Property(e => e.Repositoryshelfid).HasColumnName("repositoryshelfid");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'")
                    .HasComment("0:已删除 1：正常 2：已出库");

                entity.Property(e => e.Volume)
                    .HasPrecision(10)
                    .HasColumnName("volume");

                entity.Property(e => e.Weight)
                    .HasPrecision(10)
                    .HasColumnName("weight");
            });

            modelBuilder.Entity<Packagedetail>(entity =>
            {
                entity.ToTable("packagedetails");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Code)
                    .HasMaxLength(45)
                    .HasColumnName("code");

                entity.Property(e => e.Count).HasColumnName("count");

                entity.Property(e => e.Hasprinttags).HasColumnName("hasprinttags");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Location)
                    .HasMaxLength(45)
                    .HasColumnName("location");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("name");

                entity.Property(e => e.Packageid).HasColumnName("packageid");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Unit)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("unit");
            });

            modelBuilder.Entity<Packageexchangeapply>(entity =>
            {
                entity.ToTable("packageexchangeapply");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Appuser).HasColumnName("appuser");

                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("code");

                entity.Property(e => e.Exchangestatus).HasColumnName("exchangestatus");

                entity.Property(e => e.Exclaimid).HasColumnName("exclaimid");

                entity.Property(e => e.Lasttime).HasColumnName("lasttime");

                entity.Property(e => e.Lastuser)
                    .HasColumnType("datetime")
                    .HasColumnName("lastuser")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Packageid).HasColumnName("packageid");

                entity.Property(e => e.Remark)
                    .HasMaxLength(45)
                    .HasColumnName("remark");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Packageimage>(entity =>
            {
                entity.ToTable("packageimages");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Fileinfoid).HasColumnName("fileinfoid");

                entity.Property(e => e.Packageid).HasColumnName("packageid");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Packagenote>(entity =>
            {
                entity.ToTable("packagenote");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Isclosed).HasColumnName("isclosed");

                entity.Property(e => e.Operator)
                    .HasColumnName("operator")
                    .HasComment("0 - 无效操作 1- 入库扫描 2-包裹入库 3-用户申请发货 4-用户申请退货 5-用户申请换货  6-已出货 7-已退货 8-已换货 11-用户申请验货 12-验货完毕");

                entity.Property(e => e.Operatoruser).HasColumnName("operatoruser");

                entity.Property(e => e.Packageid).HasColumnName("packageid");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Packageorderapply>(entity =>
            {
                entity.ToTable("packageorderapply");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addressid).HasColumnName("addressid");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Agentpaytime)
                    .HasColumnType("datetime")
                    .HasColumnName("agentpaytime");

                entity.Property(e => e.Appuser).HasColumnName("appuser");

                entity.Property(e => e.Channelid).HasColumnName("channelid");

                entity.Property(e => e.Code)
                    .HasMaxLength(45)
                    .HasColumnName("code");

                entity.Property(e => e.Isagentpayed).HasColumnName("isagentpayed");

                entity.Property(e => e.Ispayed)
                    .HasColumnName("ispayed")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Nationid).HasColumnName("nationid");

                entity.Property(e => e.Orderstatus)
                    .HasColumnName("orderstatus")
                    .HasComment("1-发起申请 2-出货录单 3-确认发货 4-已发货 ");

                entity.Property(e => e.Packtime)
                    .HasColumnType("datetime")
                    .HasColumnName("packtime");

                entity.Property(e => e.Packuser).HasColumnName("packuser");

                entity.Property(e => e.Paytime)
                    .HasColumnType("datetime")
                    .HasColumnName("paytime");

                entity.Property(e => e.Remark)
                    .HasMaxLength(500)
                    .HasColumnName("remark");

                entity.Property(e => e.Sendtime)
                    .HasColumnType("datetime")
                    .HasColumnName("sendtime");

                entity.Property(e => e.Senduser).HasColumnName("senduser");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Packageorderapplydetail>(entity =>
            {
                entity.ToTable("packageorderapplydetails");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Exclaimid).HasColumnName("exclaimid");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Packageid).HasColumnName("packageid");

                entity.Property(e => e.Packageorderapplyid).HasColumnName("packageorderapplyid");

                entity.Property(e => e.Remark)
                    .HasMaxLength(500)
                    .HasColumnName("remark");

                entity.Property(e => e.Status).HasColumnName("status");
            });

            modelBuilder.Entity<Packageorderapplyerror>(entity =>
            {
                entity.ToTable("packageorderapplyerror");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Appuser).HasColumnName("appuser");

                entity.Property(e => e.Endremark)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnName("endremark")
                    .HasDefaultValueSql("''");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnName("name")
                    .HasDefaultValueSql("''");

                entity.Property(e => e.Packageorderapplyid).HasColumnName("packageorderapplyid");

                entity.Property(e => e.Prestatus).HasColumnName("prestatus");

                entity.Property(e => e.Remark)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnName("remark")
                    .HasDefaultValueSql("''");

                entity.Property(e => e.Status).HasColumnName("status");
            });

            modelBuilder.Entity<Packageorderapplyerrorlog>(entity =>
            {
                entity.ToTable("packageorderapplyerrorlog");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Appuser).HasColumnName("appuser");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Packageorderapplyerrorid).HasColumnName("packageorderapplyerrorid");

                entity.Property(e => e.Remark)
                    .HasMaxLength(500)
                    .HasColumnName("remark");

                entity.Property(e => e.Status).HasColumnName("status");

                entity.Property(e => e.Type)
                    .HasColumnName("type")
                    .HasComment("1-管理平台 2-APP用户");
            });

            modelBuilder.Entity<Packageorderapplyexpress>(entity =>
            {
                entity.ToTable("packageorderapplyexpress");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Agentcurrencychangerate)
                    .HasPrecision(10, 2)
                    .HasColumnName("agentcurrencychangerate");

                entity.Property(e => e.Agentcurrencychangerateid).HasColumnName("agentcurrencychangerateid");

                entity.Property(e => e.Agentid).HasColumnName("agentid");

                entity.Property(e => e.Agentprice)
                    .HasPrecision(10, 2)
                    .HasColumnName("agentprice");

                entity.Property(e => e.Batchnumber)
                    .HasMaxLength(45)
                    .HasColumnName("batchnumber");

                entity.Property(e => e.Couriercode)
                    .HasMaxLength(45)
                    .HasColumnName("couriercode");

                entity.Property(e => e.Courierid).HasColumnName("courierid");

                entity.Property(e => e.Currencychangerate)
                    .HasPrecision(10, 2)
                    .HasColumnName("currencychangerate");

                entity.Property(e => e.Currencychangerateid).HasColumnName("currencychangerateid");

                entity.Property(e => e.Freightprice)
                    .HasPrecision(10, 2)
                    .HasColumnName("freightprice");

                entity.Property(e => e.Innernumber)
                    .HasMaxLength(45)
                    .HasColumnName("innernumber");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Localagentprice)
                    .HasPrecision(10, 2)
                    .HasColumnName("localagentprice");

                entity.Property(e => e.Mailcode)
                    .HasMaxLength(45)
                    .HasColumnName("mailcode");

                entity.Property(e => e.Outnumber)
                    .HasMaxLength(45)
                    .HasColumnName("outnumber");

                entity.Property(e => e.Packageorderapplyid).HasColumnName("packageorderapplyid");

                entity.Property(e => e.Price)
                    .HasPrecision(10, 2)
                    .HasColumnName("price");

                entity.Property(e => e.Remark)
                    .HasMaxLength(500)
                    .HasColumnName("remark");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Targetprice)
                    .HasPrecision(10, 2)
                    .HasColumnName("targetprice");

                entity.Property(e => e.Totalcount).HasColumnName("totalcount");

                entity.Property(e => e.Totalweight)
                    .HasPrecision(10, 2)
                    .HasColumnName("totalweight");
            });

            modelBuilder.Entity<Packageorderapplyexpressdetail>(entity =>
            {
                entity.ToTable("packageorderapplyexpressdetails");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Batchnumber)
                    .HasMaxLength(45)
                    .HasColumnName("batchnumber");

                entity.Property(e => e.Bounceprice)
                    .HasPrecision(10, 2)
                    .HasColumnName("bounceprice");

                entity.Property(e => e.Boxprice)
                    .HasPrecision(10, 2)
                    .HasColumnName("boxprice");

                entity.Property(e => e.Count).HasColumnName("count");

                entity.Property(e => e.Currencychangerate)
                    .HasPrecision(10, 2)
                    .HasColumnName("currencychangerate");

                entity.Property(e => e.Currencychangerateid).HasColumnName("currencychangerateid");

                entity.Property(e => e.Customprice)
                    .HasPrecision(10, 2)
                    .HasColumnName("customprice");

                entity.Property(e => e.Forseerecieveday).HasColumnName("forseerecieveday");

                entity.Property(e => e.Haselectrified).HasColumnName("haselectrified");

                entity.Property(e => e.Height)
                    .HasPrecision(10, 2)
                    .HasColumnName("height");

                entity.Property(e => e.Innernumber)
                    .HasMaxLength(45)
                    .HasColumnName("innernumber");

                entity.Property(e => e.Length)
                    .HasPrecision(10, 2)
                    .HasColumnName("length");

                entity.Property(e => e.Outnumber)
                    .HasMaxLength(45)
                    .HasColumnName("outnumber");

                entity.Property(e => e.Overlengthprice)
                    .HasPrecision(10, 2)
                    .HasColumnName("overlengthprice");

                entity.Property(e => e.Oversizeprice)
                    .HasPrecision(10, 2)
                    .HasColumnName("oversizeprice");

                entity.Property(e => e.Overweightprice)
                    .HasPrecision(10, 2)
                    .HasColumnName("overweightprice");

                entity.Property(e => e.Packaddprice)
                    .HasPrecision(10, 2)
                    .HasColumnName("packaddprice");

                entity.Property(e => e.Packageorderapplyexpressid).HasColumnName("packageorderapplyexpressid");

                entity.Property(e => e.Paperprice)
                    .HasPrecision(10, 2)
                    .HasColumnName("paperprice");

                entity.Property(e => e.Price)
                    .HasPrecision(10, 2)
                    .HasColumnName("price");

                entity.Property(e => e.Remoteprice)
                    .HasPrecision(10, 2)
                    .HasColumnName("remoteprice");

                entity.Property(e => e.Sueprice)
                    .HasPrecision(10, 2)
                    .HasColumnName("sueprice");

                entity.Property(e => e.Targetprice)
                    .HasPrecision(10, 2)
                    .HasColumnName("targetprice");

                entity.Property(e => e.Vacuumprice)
                    .HasPrecision(10, 2)
                    .HasColumnName("vacuumprice");

                entity.Property(e => e.Volumeweight)
                    .HasPrecision(10, 2)
                    .HasColumnName("volumeweight");

                entity.Property(e => e.Weight)
                    .HasPrecision(10, 2)
                    .HasColumnName("weight");

                entity.Property(e => e.Width)
                    .HasPrecision(10, 2)
                    .HasColumnName("width");
            });

            modelBuilder.Entity<Packageorderapplyexpresspackage>(entity =>
            {
                entity.ToTable("packageorderapplyexpresspackages");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Packageid).HasColumnName("packageid");

                entity.Property(e => e.Packageorderapplyexpressdetailsid).HasColumnName("packageorderapplyexpressdetailsid");
            });

            modelBuilder.Entity<Packageorderapplyexpressstatus>(entity =>
            {
                entity.ToTable("packageorderapplyexpressstatus");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime).HasColumnType("datetime");

                entity.Property(e => e.Location)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnName("location");

                entity.Property(e => e.Packageorderapplyexpressid).HasColumnName("packageorderapplyexpressid");

                entity.Property(e => e.Searchtime)
                    .HasColumnType("datetime")
                    .HasColumnName("searchtime");
            });

            modelBuilder.Entity<Packageorderapplynote>(entity =>
            {
                entity.ToTable("packageorderapplynote");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Isclosed).HasColumnName("isclosed");

                entity.Property(e => e.Operator)
                    .HasColumnName("operator")
                    .HasComment("0 - 无效操作 1- 入库扫描 2-包裹入库 3-用户申请发货 4-用户申请退货 5-用户申请换货  6-已出货 7-已退货 8-已换货 11-用户申请验货 12-验货完毕");

                entity.Property(e => e.Operatoruser).HasColumnName("operatoruser");

                entity.Property(e => e.Packageorderapplyid).HasColumnName("packageorderapplyid");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Packagerefundapply>(entity =>
            {
                entity.ToTable("packagerefundapply");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Appuser).HasColumnName("appuser");

                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("code");

                entity.Property(e => e.Exclaimid).HasColumnName("exclaimid");

                entity.Property(e => e.Lasttime).HasColumnName("lasttime");

                entity.Property(e => e.Lastuser)
                    .HasColumnType("datetime")
                    .HasColumnName("lastuser")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Packageid).HasColumnName("packageid");

                entity.Property(e => e.Refundstatus).HasColumnName("refundstatus");

                entity.Property(e => e.Remark)
                    .HasMaxLength(45)
                    .HasColumnName("remark");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Packagevideo>(entity =>
            {
                entity.ToTable("packagevideos");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Fileinfoid).HasColumnName("fileinfoid");

                entity.Property(e => e.Packageid).HasColumnName("packageid");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Refundorder>(entity =>
            {
                entity.ToTable("refundorder");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("code");

                entity.Property(e => e.Expressclaimid).HasColumnName("expressclaimid");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Repository>(entity =>
            {
                entity.ToTable("repository");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Address)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("address");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Companyid).HasColumnName("companyid");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("name");

                entity.Property(e => e.Recivermobil)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("recivermobil");

                entity.Property(e => e.Recivername)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("recivername");

                entity.Property(e => e.Region)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("region");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Repositorylayer>(entity =>
            {
                entity.ToTable("repositorylayer");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("name");

                entity.Property(e => e.Order).HasColumnName("order");

                entity.Property(e => e.Repositoryid).HasColumnName("repositoryid");

                entity.Property(e => e.Repositoryshelfid).HasColumnName("repositoryshelfid");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Repositoryregion>(entity =>
            {
                entity.ToTable("repositoryregion");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("name");

                entity.Property(e => e.Order).HasColumnName("order");

                entity.Property(e => e.Repositoryid).HasColumnName("repositoryid");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Repositoryshelf>(entity =>
            {
                entity.ToTable("repositoryshelf");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("name");

                entity.Property(e => e.Order).HasColumnName("order");

                entity.Property(e => e.Repositoryid).HasColumnName("repositoryid");

                entity.Property(e => e.Repositoryregionid).HasColumnName("repositoryregionid");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Runfee>(entity =>
            {
                entity.ToTable("runfee");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Accountid).HasColumnName("accountid");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("name");

                entity.Property(e => e.Paytime)
                    .HasColumnType("datetime")
                    .HasColumnName("paytime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Paytype)
                    .HasColumnName("paytype")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Status).HasColumnName("status");
            });

            modelBuilder.Entity<Syscompany>(entity =>
            {
                entity.ToTable("syscompany");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("name");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Sysdepartment>(entity =>
            {
                entity.ToTable("sysdepartment");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Companyid).HasColumnName("companyid");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("name");

                entity.Property(e => e.Order).HasColumnName("order");

                entity.Property(e => e.Parentid).HasColumnName("parentid");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Sysfunction>(entity =>
            {
                entity.ToTable("sysfunction");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("name");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Typename)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("typename")
                    .HasComment("C#特性名称");
            });

            modelBuilder.Entity<Sysmenu>(entity =>
            {
                entity.ToTable("sysmenu");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Index)
                    .HasMaxLength(200)
                    .HasColumnName("index");

                entity.Property(e => e.Isdirectory).HasColumnName("isdirectory");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("name");

                entity.Property(e => e.Order).HasColumnName("order");

                entity.Property(e => e.Parentid).HasColumnName("parentid");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Sysrole>(entity =>
            {
                entity.ToTable("sysrole");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Companyid).HasColumnName("companyid");

                entity.Property(e => e.Isdefault).HasColumnName("isdefault");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("name");

                entity.Property(e => e.Order).HasColumnName("order");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Sysrolemenufunction>(entity =>
            {
                entity.ToTable("sysrolemenufunction");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Companyid).HasColumnName("companyid");

                entity.Property(e => e.Functionid).HasColumnName("functionid");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Menuid).HasColumnName("menuid");

                entity.Property(e => e.Roleid).HasColumnName("roleid");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Syssetting>(entity =>
            {
                entity.ToTable("syssetting");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("code");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("name");

                entity.Property(e => e.Value)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("value");
            });

            modelBuilder.Entity<Sysuser>(entity =>
            {
                entity.ToTable("sysuser");

                entity.HasIndex(e => e.Name, "index_su_name");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Mobile)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("mobile");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("name")
                    .HasComment("登录名");

                entity.Property(e => e.Password)
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnName("password");

                entity.Property(e => e.Sex).HasColumnName("sex");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Truename)
                    .HasMaxLength(45)
                    .HasColumnName("truename");
            });

            modelBuilder.Entity<Sysusercompany>(entity =>
            {
                entity.ToTable("sysusercompany");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Companyid).HasColumnName("companyid");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Userid).HasColumnName("userid");
            });

            modelBuilder.Entity<Sysuserdepartment>(entity =>
            {
                entity.ToTable("sysuserdepartment");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Companyid).HasColumnName("companyid");

                entity.Property(e => e.Departmentid).HasColumnName("departmentid");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Order).HasColumnName("order");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Userid).HasColumnName("userid");
            });

            modelBuilder.Entity<Sysuserrole>(entity =>
            {
                entity.ToTable("sysuserrole");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Companyid).HasColumnName("companyid");

                entity.Property(e => e.Lasttime)
                    .HasColumnType("datetime")
                    .HasColumnName("lasttime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Lastuser).HasColumnName("lastuser");

                entity.Property(e => e.Roleid).HasColumnName("roleid");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Userid).HasColumnName("userid");
            });

            modelBuilder.Entity<Viewagentuserdatum>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("viewagentuserdata");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("'0000-00-00 00:00:00'");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Agentid).HasColumnName("agentid");

                entity.Property(e => e.Amount)
                    .HasPrecision(12, 2)
                    .HasColumnName("amount");

                entity.Property(e => e.Currencyid).HasColumnName("currencyid");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Orderid).HasColumnName("orderid");

                entity.Property(e => e.Paytype).HasColumnName("paytype");
            });

            modelBuilder.Entity<Viewappuserdatum>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("viewappuserdata");

                entity.Property(e => e.Addtime)
                    .HasColumnType("datetime")
                    .HasColumnName("addtime")
                    .HasDefaultValueSql("'0000-00-00 00:00:00'");

                entity.Property(e => e.Adduser).HasColumnName("adduser");

                entity.Property(e => e.Amount)
                    .HasPrecision(10, 2)
                    .HasColumnName("amount");

                entity.Property(e => e.Appuser).HasColumnName("appuser");

                entity.Property(e => e.Currencyid).HasColumnName("currencyid");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Orderid).HasColumnName("orderid");

                entity.Property(e => e.Paytype).HasColumnName("paytype");

                entity.Property(e => e.Type).HasColumnName("type");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
