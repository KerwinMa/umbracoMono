using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using SqlCE4Umbraco;
using umbraco.DataLayer.SqlHelpers.MySqlTest;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.IO;
using log4net.Config;
using umbraco.DataLayer;
using GlobalSettings = umbraco.GlobalSettings;

namespace Umbraco.Tests.TestHelpers
{
	/// <summary>
	/// Common helper properties and methods useful to testing
	/// </summary>
	public static class TestHelper
	{
		public const string umbracoDbDsn = @"server=127.0.0.1;database=umbraco6_test;user id=umbracouser;password=P@ssword1;datalayer=MySqlTest";

		private static MySqlTestHelper GetDataHelper()
		{
			var databaseSettings = ConfigurationManagerProvider.Instance.GetConfigManager().ConnectionStrings[Core.Configuration.GlobalSettings.UmbracoConnectionName];
			var dataHelper = DataLayerHelper.CreateSqlHelper(umbracoDbDsn, true) as MySqlTestHelper;

			if (dataHelper == null)
				throw new InvalidOperationException("The sql helper for unit tests must be of type MySqlTestHelper, check the ensure the connection string used for this test is set to use MySqlTest");
		
			return dataHelper;
		}

		/// <summary>
		/// Clears an initialized database
		/// </summary>		
		public static void ClearDatabase()
		{
			GetDataHelper().ClearDatabase();
		}

        public static void DropForeignKeys(string table)
        {
			GetDataHelper().DropForeignKeys(table);
        }

		/// <summary>
		/// Initializes a new database
		/// </summary>
		public static void InitializeDatabase()
		{
			var dataHelper = GetDataHelper();

			dataHelper.ClearDatabase();

			var installer = dataHelper.Utility.CreateInstaller();
			if (installer.CanConnect)
			{
				installer.Install();
			}
		}

		/// <summary>
		/// Gets the current assembly directory.
		/// </summary>
		/// <value>The assembly directory.</value>
		static public string CurrentAssemblyDirectory
		{
			get
			{
				var codeBase = typeof(TestHelper).Assembly.CodeBase;
				var uri = new Uri(codeBase);
				var path = uri.LocalPath;
				return Path.GetDirectoryName(path);
			}
		}

		/// <summary>
		/// Maps the given <paramref name="relativePath"/> making it rooted on <see cref="CurrentAssemblyDirectory"/>. <paramref name="relativePath"/> must start with <code>~/</code>
		/// </summary>
		/// <param name="relativePath">The relative path.</param>
		/// <returns></returns>
		public static string MapPathForTest(string relativePath)
		{
			if (!relativePath.StartsWith("~/"))
				throw new ArgumentException("relativePath must start with '~/'", "relativePath");

			return relativePath.Replace("~/", CurrentAssemblyDirectory + "/");
		}

		public static void SetupLog4NetForTests()
		{
			XmlConfigurator.Configure(new FileInfo(MapPathForTest("~/unit-test-log4net.config")));
		}

        public static void InitializeContentDirectories()
        {
            CreateDirectories(new[] { SystemDirectories.Masterpages, SystemDirectories.MvcViews, SystemDirectories.Media });
        }

	    public static void CleanContentDirectories()
	    {
	        CleanDirectories(new[] { SystemDirectories.Masterpages, SystemDirectories.MvcViews, SystemDirectories.Media });
	    }

	    public static void CreateDirectories(string[] directories)
        {
            foreach (var directory in directories)
            {
                var directoryInfo = new DirectoryInfo(IOHelper.MapPath(directory));
                if (directoryInfo.Exists == false)
                    Directory.CreateDirectory(IOHelper.MapPath(directory));
            }
        }

	    public static void CleanDirectories(string[] directories)
        {
            foreach (var directory in directories)
            {
                var directoryInfo = new DirectoryInfo(IOHelper.MapPath(directory));
                if (directoryInfo.Exists)
                    directoryInfo.GetFiles().ForEach(x => x.Delete());
            }
        }
	}
}