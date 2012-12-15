﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Rdbms;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.Repositories;
using Umbraco.Core.Persistence.UnitOfWork;
using Umbraco.Core.Services;
using Umbraco.Tests.TestHelpers;
using Umbraco.Tests.TestHelpers.Entities;
using umbraco.editorControls.tinyMCE3;
using umbraco.interfaces;

namespace Umbraco.Tests.Services
{
	/// <summary>
    /// Tests covering all methods in the ContentService class.
    /// This is more of an integration test as it involves multiple layers
    /// as well as configuration.
    /// </summary>
    [TestFixture, RequiresSTA]
    public class ContentServiceTests : BaseDatabaseFactoryTest
    {
        [SetUp]
        public override void Initialize()
        {

            //this ensures its reset
            PluginManager.Current = new PluginManager();

            //for testing, we'll specify which assemblies are scanned for the PluginTypeResolver
            PluginManager.Current.AssembliesToScan = new[]
				{
                    typeof(IDataType).Assembly,
                    typeof(tinyMCE3dataType).Assembly
				};

            DataTypesResolver.Current = new DataTypesResolver(
                PluginManager.Current.ResolveDataTypes());

            base.Initialize();

            CreateTestData();
        }
		
		[TearDown]
		public override void TearDown()
		{
            //reset the app context
            DataTypesResolver.Reset();
            PluginManager.Current = null;

			base.TearDown();
		}

        //TODO Add test to verify there is only ONE newest document/content in cmsDocument table after updating.
        //TODO Add test to delete specific version (with and without deleting prior versions) and versions by date.

        [Test]
        public void Can_Create_Content()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;

            // Act
            var content = contentService.CreateContent(-1, "umbTextpage", 0);

            // Assert
            Assert.That(content, Is.Not.Null);
            Assert.That(content.HasIdentity, Is.False);
        }

        [Test]
        public void Can_Create_Content_Using_HttpContext_To_Set_User()
        {
            // Arrange
            var userId =
                Convert.ToInt32(
                    DatabaseContext.Database.Insert(new UserDto
                                                        {
                                                            ContentStartId = -1,
                                                            DefaultPermissions = null,
                                                            DefaultToLiveEditing = false,
                                                            Disabled = false,
                                                            Email = "my@email.com",
                                                            Login = "editor",
                                                            MediaStartId = -1,
                                                            NoConsole = false,
                                                            Password = "1234",
                                                            Type = 3,
                                                            UserLanguage = "en",
                                                            UserName = "John Doe the Editor"
                                                        }));

            DatabaseContext.Database.Insert(new UserLoginDto
                                                {
                                                    UserId = userId,
                                                    ContextId = new Guid("FBA996E7-D6BE-489B-B199-2B0F3D2DD826"),
                                                    Timeout = 634596443995451258
                                                });

            var contentService = ServiceContext.ContentService as ContentService;
            contentService.SetHttpContext(base.GetUmbracoContext("/test", 1234).HttpContext);

            // Act
            var content = contentService.CreateContent(-1, "umbTextpage");

            // Assert
            Assert.That(content, Is.Not.Null);
            Assert.That(content.HasIdentity, Is.False);
            Assert.That(content.CreatorId, Is.EqualTo(userId));
        }

        [Test]
        public void Can_Create_Content_Without_HttpContext_To_Set_User()
        {
            // Arrange
            var userId =
                Convert.ToInt32(
                    DatabaseContext.Database.Insert(new UserDto
                                                        {
                                                            ContentStartId = -1,
                                                            DefaultPermissions = null,
                                                            DefaultToLiveEditing = false,
                                                            Disabled = false,
                                                            Email = "my@email.com",
                                                            Login = "editor",
                                                            MediaStartId = -1,
                                                            NoConsole = false,
                                                            Password = "1234",
                                                            Type = 3,
                                                            UserLanguage = "en",
                                                            UserName = "John Doe the Editor"
                                                        }));

            DatabaseContext.Database.Insert(new UserLoginDto
                                                {
                                                    UserId = userId,
                                                    ContextId = new Guid("FBA996E7-D6BE-489B-B199-2B0F3D2DD826"),
                                                    Timeout = 634596443995451258
                                                });

            // Act
            var contentService = ServiceContext.ContentService as ContentService;
            contentService.SetHttpContext(null);
            var content = contentService.CreateContent(-1, "umbTextpage");

            // Assert
            Assert.That(content, Is.Not.Null);
            Assert.That(content.HasIdentity, Is.False);
            Assert.That(content.CreatorId, Is.EqualTo(0));//Default to zero/administrator
        }

        [Test]
        public void Cannot_Create_Content_With_Non_Existing_ContentType_Alias()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;

            // Act & Assert
            Assert.Throws<Exception>(() => contentService.CreateContent(-1, "umbAliasDoesntExist"));
        }

        [Test]
        public void Can_Get_Content_By_Id()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;

            // Act
            var content = contentService.GetById(1046);

            // Assert
            Assert.That(content, Is.Not.Null);
            Assert.That(content.Id, Is.EqualTo(1046));
        }

        [Test]
        public void Can_Get_Content_By_Guid_Key()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;

            // Act
            var content = contentService.GetById(new Guid("B58B3AD4-62C2-4E27-B1BE-837BD7C533E0"));

            // Assert
            Assert.That(content, Is.Not.Null);
            Assert.That(content.Id, Is.EqualTo(1046));
        }

        [Test]
        public void Can_Get_Content_By_Level()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;

            // Act
            var contents = contentService.GetByLevel(2);

            // Assert
            Assert.That(contents, Is.Not.Null);
            Assert.That(contents.Any(), Is.True);
            Assert.That(contents.Count(), Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void Can_Get_Children_Of_Content_Id()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;

            // Act
            var contents = contentService.GetChildren(1046);

            // Assert
            Assert.That(contents, Is.Not.Null);
            Assert.That(contents.Any(), Is.True);
            Assert.That(contents.Count(), Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void Can_Get_All_Versions_Of_Content()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;
            var parent = ServiceContext.ContentService.GetById(1046);
            ServiceContext.ContentService.Publish(parent);//Publishing root, so Text Page 2 can be updated.
            var subpage2 = contentService.GetById(1048);
            subpage2.Name = "Text Page 2 Updated";
            subpage2.SetValue("author", "Jane Doe");
            contentService.SaveAndPublish(subpage2, 0);//NOTE New versions are only added between publish-state-changed, so publishing to ensure addition version.

            // Act
            var versions = contentService.GetVersions(1048);

            // Assert
            Assert.That(versions.Any(), Is.True);
            Assert.That(versions.Count(), Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void Can_Get_Root_Content()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;

            // Act
            var contents = contentService.GetRootContent();

            // Assert
            Assert.That(contents, Is.Not.Null);
            Assert.That(contents.Any(), Is.True);
            Assert.That(contents.Count(), Is.EqualTo(1));
        }

        [Test]
        public void Can_Get_Content_For_Expiration()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;
            var root = contentService.GetById(1046);
            contentService.SaveAndPublish(root);
            var content = contentService.GetById(1048);
            content.ExpireDate = DateTime.UtcNow.AddSeconds(1);
            contentService.SaveAndPublish(content);

            // Act
            Thread.Sleep(new TimeSpan(0, 0, 0, 2));
            var contents = contentService.GetContentForExpiration();

            // Assert
            Assert.That(DateTime.UtcNow.AddMinutes(-5) <= DateTime.UtcNow);
            Assert.That(contents, Is.Not.Null);
            Assert.That(contents.Any(), Is.True);
            Assert.That(contents.Count(), Is.EqualTo(1));
        }

        [Test]
        public void Can_Get_Content_For_Release()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;

            // Act
            var contents = contentService.GetContentForRelease();

            // Assert
            Assert.That(DateTime.UtcNow.AddMinutes(-5) <= DateTime.UtcNow);
            Assert.That(contents, Is.Not.Null);
            Assert.That(contents.Any(), Is.True);
            Assert.That(contents.Count(), Is.EqualTo(1));
        }

        [Test]
        public void Can_Get_Content_In_RecycleBin()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;

            // Act
            var contents = contentService.GetContentInRecycleBin();

            // Assert
            Assert.That(contents, Is.Not.Null);
            Assert.That(contents.Any(), Is.True);
            Assert.That(contents.Count(), Is.EqualTo(1));
        }

        [Test]
        public void Can_UnPublish_Content()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;
            var content = contentService.GetById(1046);
            bool published = contentService.Publish(content, 0);

            // Act
            bool unpublished = contentService.UnPublish(content, 0);

            // Assert
            Assert.That(published, Is.True);
            Assert.That(unpublished, Is.True);
            Assert.That(content.Published, Is.False);
        }

        [Test]
        public void Can_UnPublish_Root_Content_And_Verify_Children_Is_UnPublished()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;
            var published = contentService.RePublishAll(0);
            var content = contentService.GetById(1046);

            // Act
            bool unpublished = contentService.UnPublish(content, 0);
            var children = contentService.GetChildren(1046);

            // Assert
            Assert.That(published, Is.True);//Verify that everything was published
            
            //Verify that content with Id 1046 was unpublished
            Assert.That(unpublished, Is.True);
            Assert.That(content.Published, Is.False);

            //Verify that all children was unpublished
            Assert.That(children.Any(x => x.Published), Is.False);
            Assert.That(children.First(x => x.Id == 1047).Published, Is.False);//Released 5 mins ago, but should be unpublished
            Assert.That(children.First(x => x.Id == 1047).ReleaseDate.HasValue, Is.False);//Verify that the release date has been removed
            Assert.That(children.First(x => x.Id == 1048).Published, Is.False);//Expired 5 mins ago, so isn't be published
        }

        [Test]
        public void Can_RePublish_All_Content()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;
            var contentTypeService = ServiceContext.ContentTypeService;
            var contentType = contentTypeService.GetContentType("umbTextpage");

            // Act
            var published = contentService.RePublishAll(0);
            var contents = contentService.GetContentOfContentType(contentType.Id);

            // Assert
            Assert.That(published, Is.True);
            Assert.That(contents.First(x => x.Id == 1046).Published, Is.True);//No restrictions, so should be published
            Assert.That(contents.First(x => x.Id == 1047).Published, Is.True);//Released 5 mins ago, so should be published
            Assert.That(contents.First(x => x.Id == 1049).Published, Is.False);//Trashed content, so shouldn't be published
        }

        [Test]
        public void Can_Publish_Content()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;
            var content = contentService.GetById(1046);

            // Act
            bool published = contentService.Publish(content, 0);

            // Assert
            Assert.That(published, Is.True);
            Assert.That(content.Published, Is.True);
        }

        [Test]
        public void Can_Publish_Only_Valid_Content()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;
            var contentTypeService = ServiceContext.ContentTypeService;
            var contentType = MockedContentTypes.CreateSimpleContentType("umbMandatory", "Mandatory Doc Type", true);
            contentTypeService.Save(contentType);

            Content content = MockedContent.CreateSimpleContent(contentType, "Invalid Content", 1046);
            content.SetValue("author", string.Empty);
            contentService.Save(content, 0);

            // Act
            var parent = contentService.GetById(1046);
            bool parentPublished = contentService.Publish(parent, 0);
            bool published = contentService.Publish(content, 0);

            // Assert
            Assert.That(parentPublished, Is.True);
            Assert.That(published, Is.False);
            Assert.That(content.IsValid(), Is.False);
            Assert.That(parent.Published, Is.True);
            Assert.That(content.Published, Is.False);
        }

        [Test]
        public void Can_Publish_Content_Children()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;
            var content = contentService.GetById(1046);

            // Act
            bool published = contentService.PublishWithChildren(content, 0);
            var children = contentService.GetChildren(1046);

            // Assert
            Assert.That(published, Is.True);//Nothing was cancelled, so should be true
            Assert.That(content.Published, Is.True);//No restrictions, so should be published
            Assert.That(children.First(x => x.Id == 1047).Published, Is.True);//Released 5 mins ago, so should be published
        }

        [Test]
        public void Cannot_Publish_Expired_Content()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;
            var content = contentService.GetById(1048); //This Content expired 5min ago
            content.ExpireDate = DateTime.UtcNow.AddMinutes(-5);
            contentService.Save(content);

            var parent = contentService.GetById(1046);
            bool parentPublished = contentService.Publish(parent, 0);//Publish root Home node to enable publishing of '1048'

            // Act
            bool published = contentService.Publish(content, 0);

            // Assert
            Assert.That(parentPublished, Is.True);
            Assert.That(published, Is.False);
            Assert.That(content.Published, Is.False);
        }

        [Test]
        public void Cannot_Publish_Content_Awaiting_Release()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;
            var content = contentService.GetById(1047);
            content.ReleaseDate = DateTime.UtcNow.AddHours(2);
            contentService.Save(content, 0);

            var parent = contentService.GetById(1046);
            bool parentPublished = contentService.Publish(parent, 0);//Publish root Home node to enable publishing of '1048'

            // Act
            bool published = contentService.Publish(content, 0);

            // Assert
            Assert.That(parentPublished, Is.True);
            Assert.That(published, Is.False);
            Assert.That(content.Published, Is.False);
        }

        [Test]
        public void Cannot_Publish_Content_Where_Parent_Is_Unpublished()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;
            var content = contentService.CreateContent(1046, "umbTextpage", 0);
            content.Name = "Subpage with Unpublisehed Parent";
            contentService.Save(content, 0);

            // Act
            bool published = contentService.PublishWithChildren(content, 0);

            // Assert
            Assert.That(published, Is.False);
            Assert.That(content.Published, Is.False);
        }

        [Test]
        public void Cannot_Publish_Trashed_Content()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;
            var content = contentService.GetById(1049);

            // Act
            bool published = contentService.Publish(content, 0);

            // Assert
            Assert.That(published, Is.False);
            Assert.That(content.Published, Is.False);
            Assert.That(content.Trashed, Is.True);
        }

        [Test]
        public void Can_Save_And_Publish_Content()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;
            var content = contentService.CreateContent(-1, "umbTextpage", 0);
            content.Name = "Home US";
            content.SetValue("author", "Barack Obama");

            // Act
            bool published = contentService.SaveAndPublish(content, 0);

            // Assert
            Assert.That(content.HasIdentity, Is.True);
            Assert.That(content.Published, Is.True);
            Assert.That(published, Is.True);
        }

        [Test]
        public void Can_Save_Content()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;
            var content = contentService.CreateContent(-1, "umbTextpage", 0);
            content.Name = "Home US";
            content.SetValue("author", "Barack Obama");

            // Act
            contentService.Save(content, 0);

            // Assert
            Assert.That(content.HasIdentity, Is.True);
        }

        [Test]
        public void Can_Bulk_Save_Content()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;
            var contentTypeService = ServiceContext.ContentTypeService;

            var contentType = contentTypeService.GetContentType("umbTextpage");
            Content subpage = MockedContent.CreateSimpleContent(contentType, "Text Subpage 1", 1047);
            Content subpage2 = MockedContent.CreateSimpleContent(contentType, "Text Subpage 2", 1047);
            var list = new List<IContent> {subpage, subpage2};

            // Act
            contentService.Save(list, 0);

            // Assert
            Assert.That(list.Any(x => !x.HasIdentity), Is.False);
        }

        [Test]
        public void Can_Bulk_Save_New_Hierarchy_Content()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;
            var hierarchy = CreateContentHierarchy();

            // Act
            contentService.Save(hierarchy, 0);

            Assert.That(hierarchy.Any(), Is.True);
            Assert.That(hierarchy.Any(x => x.Value.HasIdentity == false), Is.False);
        }

        [Test]
        public void Can_Delete_Content_Of_Specific_ContentType()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;
            var contentTypeService = ServiceContext.ContentTypeService;
            var contentType = contentTypeService.GetContentType("umbTextpage");

            // Act
            contentService.DeleteContentOfType(contentType.Id);
            var rootContent = contentService.GetRootContent();
            var contents = contentService.GetContentOfContentType(contentType.Id);

            // Assert
            Assert.That(rootContent.Any(), Is.False);
            Assert.That(contents.Any(x => !x.Trashed), Is.False);
        }

        [Test]
        public void Can_Delete_Content()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;
            var content = contentService.GetById(1049);

            // Act
            contentService.Delete(content, 0);
            var deleted = contentService.GetById(1049);

            // Assert
            Assert.That(deleted, Is.Null);
        }

        [Test]
        public void Can_Move_Content_To_RecycleBin()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;
            var content = contentService.GetById(1048);

            // Act
            contentService.MoveToRecycleBin(content, 0);

            // Assert
            Assert.That(content.ParentId, Is.EqualTo(-20));
            Assert.That(content.Trashed, Is.True);
        }

        [Test]
        public void Can_Empty_RecycleBin()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;

            // Act
            contentService.EmptyRecycleBin();
            var contents = contentService.GetContentInRecycleBin();

            // Assert
            Assert.That(contents.Any(), Is.False);
        }

        [Test]
        public void Can_Move_Content()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;
            var content = contentService.GetById(1049);

            // Act - moving out of recycle bin
            contentService.Move(content, 1046, 0);

            // Assert
            Assert.That(content.ParentId, Is.EqualTo(1046));
            Assert.That(content.Trashed, Is.False);
            Assert.That(content.Published, Is.False);
        }

        [Test]
        public void Can_Copy_Content()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;
            var content = contentService.GetById(1048);

            // Act
            var copy = contentService.Copy(content, content.ParentId, false, 0);

            // Assert
            Assert.That(copy, Is.Not.Null);
            Assert.That(copy.Id, Is.Not.EqualTo(content.Id));
            Assert.AreNotSame(content, copy);
            Assert.AreNotEqual(content.Name, copy.Name);
        }

        [Test, NUnit.Framework.Ignore]
        public void Can_Send_To_Publication()
        { }

        [Test]
        public void Can_Rollback_Version_On_Content()
        {
            // Arrange
            var contentService = ServiceContext.ContentService;
            var parent = ServiceContext.ContentService.GetById(1046);
            ServiceContext.ContentService.Publish(parent);//Publishing root, so Text Page 2 can be updated.
            var subpage2 = contentService.GetById(1048);
            var version = subpage2.Version;
            var nameBeforeRollback = subpage2.Name;
            subpage2.Name = "Text Page 2 Updated";
            subpage2.SetValue("author", "Jane Doe");
            contentService.SaveAndPublish(subpage2, 0);//Saving and publishing, so a new version is created

            // Act
            var rollback = contentService.Rollback(1048, version, 0);

            // Assert
            Assert.That(rollback, Is.Not.Null);
            Assert.AreNotEqual(rollback.Version, subpage2.Version);
            Assert.That(rollback.GetValue<string>("author"), Is.Not.EqualTo("Jane Doe"));
            Assert.AreEqual(nameBeforeRollback, rollback.Name);
        }

        [Test]
        public void Can_Save_Lazy_Content()
        {	        
	        var unitOfWork = PetaPocoUnitOfWorkProvider.CreateUnitOfWork();
            var contentType = ServiceContext.ContentTypeService.GetContentType("umbTextpage");
            var root = ServiceContext.ContentService.GetById(1046);

            var c = new Lazy<IContent>(() => MockedContent.CreateSimpleContent(contentType, "Hierarchy Simple Text Page", root.Id));
            var c2 = new Lazy<IContent>(() => MockedContent.CreateSimpleContent(contentType, "Hierarchy Simple Text Subpage", c.Value.Id));
            var list = new List<Lazy<IContent>> {c, c2};

            var repository = RepositoryResolver.Current.ResolveByType<IContentRepository>(unitOfWork);
            foreach (var content in list)
            {
                repository.AddOrUpdate(content.Value);
                unitOfWork.Commit();
            }

            Assert.That(c.Value.HasIdentity, Is.True);
            Assert.That(c2.Value.HasIdentity, Is.True);

            Assert.That(c.Value.Id > 0, Is.True);
            Assert.That(c2.Value.Id > 0, Is.True);

            Assert.That(c.Value.ParentId > 0, Is.True);
            Assert.That(c2.Value.ParentId > 0, Is.True);
        }

        public void CreateTestData()
        {
            //NOTE Maybe not the best way to create/save test data as we are using the services, which are being tested.

            //Create and Save ContentType "umbTextpage" -> 1045
            ContentType contentType = MockedContentTypes.CreateSimpleContentType("umbTextpage", "Textpage");
            contentType.Key = new Guid("1D3A8E6E-2EA9-4CC1-B229-1AEE19821522");
            ServiceContext.ContentTypeService.Save(contentType);

            //Create and Save Content "Homepage" based on "umbTextpage" -> 1046
            Content textpage = MockedContent.CreateSimpleContent(contentType);
            textpage.Key = new Guid("B58B3AD4-62C2-4E27-B1BE-837BD7C533E0");
            ServiceContext.ContentService.Save(textpage, 0);

            //Create and Save Content "Text Page 1" based on "umbTextpage" -> 1047
            Content subpage = MockedContent.CreateSimpleContent(contentType, "Text Page 1", textpage.Id);
            subpage.ReleaseDate = DateTime.UtcNow.AddMinutes(-5);
            subpage.ChangePublishedState(false);
            ServiceContext.ContentService.Save(subpage, 0);

            //Create and Save Content "Text Page 1" based on "umbTextpage" -> 1048
            Content subpage2 = MockedContent.CreateSimpleContent(contentType, "Text Page 2", textpage.Id);
            ServiceContext.ContentService.Save(subpage2, 0);

            //Create and Save Content "Text Page Deleted" based on "umbTextpage" -> 1049
            Content trashed = MockedContent.CreateSimpleContent(contentType, "Text Page Deleted", -20);
            trashed.Trashed = true;
            ServiceContext.ContentService.Save(trashed, 0);
        }

        private IEnumerable<Lazy<IContent>> CreateContentHierarchy()
        {
            var contentType = ServiceContext.ContentTypeService.GetContentType("umbTextpage");
            var root = ServiceContext.ContentService.GetById(1046);

            var list = new List<Lazy<IContent>>();

            for (int i = 0; i < 10; i++)
            {
                var content = new Lazy<IContent>(
                    () => MockedContent.CreateSimpleContent(contentType, "Hierarchy Simple Text Page " + i, root.Id));
                list.Add(content);
                list.AddRange(CreateChildrenOf(contentType, content, 4));

                Console.WriteLine("Created: 'Hierarchy Simple Text Page {0}'", i);
            }

            return list;
        }

        private IEnumerable<Lazy<IContent>> CreateChildrenOf(IContentType contentType, Lazy<IContent> content, int depth)
        {
            var list = new List<Lazy<IContent>>();
            for (int i = 0; i < depth; i++)
            {
                var c = new Lazy<IContent>(() => MockedContent.CreateSimpleContent(contentType, "Hierarchy Simple Text Subpage " + i, content.Value.Id));
                list.Add(c);

                Console.WriteLine("Created: 'Hierarchy Simple Text Subpage {0}' - Depth: {1}", i, depth);
            }
            return list;
        }
    }
}