//Copyright (c) 2018 Yardi Technology Limited. Http://www.kooboo.com 
//All rights reserved.
using Kooboo.Sites.Render;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kooboo.Extensions;
using Kooboo.Sites.Models;
using Kooboo.Data.Interface;
using Kooboo.Sites.Repository;
using Kooboo.Lib.Helper;

namespace Kooboo.Sites.Systems
{
    public class SystemRender
    {
        public static void Render(FrontContext context)
        {
            //systemroute.Name = "__kb/{objecttype}/{nameorid}"; defined in Routes. 
            var paras = context.Route.Parameters;
            var constTypeString = paras.GetValue("objecttype");
            byte constType = ConstObjectType.Unknown;
            if (!byte.TryParse(constTypeString, out constType))
            {
                constType = ConstTypeContainer.GetConstType(constTypeString);
            }
            var id = paras.GetValue("nameorid");

            switch (constType)
            {
                case ConstObjectType.ResourceGroup:
                    ResourceGroupRender(context, id);
                    return;
                case ConstObjectType.View:
                    ViewRender(context, id, paras);
                    return;
                case ConstObjectType.Image:
                    {
                        ImageRender(context, id, paras);
                        return;
                    }
                case ConstObjectType.CmsFile:
                    {
                        FileRender(context, id, paras);
                        return;
                    }
                case ConstObjectType.kfile:
                    {
                        KFileRender(context, paras);
                        return; 
                    }

                //case ConstObjectType.TextContent:
                //    {
                //        TextContentRender(context, id, paras);
                //        return;
                //    }
                default:
                    DefaultRender(context, constType, id, paras);
                    break;
            }
        }

        private static string GetObjectType(Dictionary<string, string> parameters)
        {
            if (parameters.ContainsKey("objecttype"))
            {
                return parameters["objecttype"];
            }
            else if (parameters.ContainsKey("type"))
            {
                return parameters["type"];
            }
            return null;
        }

        public static void ResourceGroupRender(FrontContext context, string id)
        {
            var db = context.SiteDb;
            var group = db.ResourceGroups.GetByNameOrId(id, ConstObjectType.Style);
            if (group == null)
            {
                group = db.ResourceGroups.GetByNameOrId(id, ConstObjectType.Script);
            }
            if (group == null)
            {
                group = db.ResourceGroups.TableScan.Where(o => Lib.Helper.StringHelper.IsSameValue(o.Name, id)).FirstOrDefault();
            }
            if (group == null)
            {
                return;
            }
            IRepository repo = null;
            string spliter = "\r\n";
            switch (group.Type)
            {
                case ConstObjectType.Style:
                    context.RenderContext.Response.ContentType = "text/css;charset=utf-8";
                    repo = context.SiteDb.Styles as IRepository;
                    break;
                case ConstObjectType.Script:
                    context.RenderContext.Response.ContentType = "text/javascript;charset=utf-8";
                    repo = context.SiteDb.Scripts as IRepository;
                    spliter = ";";
                    break;
                default:
                    break;
            }

            StringBuilder sb = new StringBuilder();

            foreach (var item in group.Children.OrderBy(o => o.Value))
            {
                var route = context.SiteDb.Routes.Get(item.Key);
                if (route != null)
                {
                    var siteobject = repo.Get(route.objectId);
                    if (siteobject != null && siteobject is ITextObject)
                    {
                        var text = siteobject as ITextObject;
                        sb.Append(text.Body);
                        sb.Append(spliter);
                    }
                }
            }

            context.RenderContext.Response.Body = DataConstants.DefaultEncoding.GetBytes(sb.ToString());
        }

        public static void ViewRender(FrontContext context, string NameOrId, Dictionary<string, string> Parameters)
        {
            var action = Parameters.GetValue("action");

            if (string.IsNullOrEmpty(action) || action == "render")
            {
                System.Guid viewid = default(Guid);
                System.Guid.TryParse(NameOrId, out viewid);
                if (viewid == default(Guid))
                {
                    viewid = Kooboo.Data.IDGenerator.Generate(NameOrId, ConstObjectType.View);
                }
                context.Route.DestinationConstType = ConstObjectType.View;
                context.Route.objectId = viewid;
                ViewRenderer.Render(context);
            }
            else
            {
                var view = context.SiteDb.Views.GetByNameOrId(NameOrId);
                if (view != null)
                {
                    context.RenderContext.Response.ContentType = "text/html;charset=utf-8";
                    context.RenderContext.Response.Body = DataConstants.DefaultEncoding.GetBytes(view.Body);
                }
            }
        }

        public static void ImageRender(FrontContext context, string NameOrId, Dictionary<string, string> Parameters)
        {

            Guid ImageId;
            Image image = null;

            if (System.Guid.TryParse(NameOrId, out ImageId))
            {
                image = context.SiteDb.Images.Get(ImageId) as Image;
            }

            long logid = -1;

            if (long.TryParse(NameOrId, out logid))
            {
                var logentry = context.SiteDb.Log.Get(logid);
                var repo = context.SiteDb.Images as IRepository;
                image = repo.GetByLog(logentry) as Image;
            }

            if (image != null)
            {
                Kooboo.Sites.Render.ImageRenderer.RenderImage(context, image);
            }
        }

        public static void FileRender(FrontContext context, string NameOrId, Dictionary<string, string> Parameters)
        {
            Guid FileId;
            CmsFile file = null;

            if (System.Guid.TryParse(NameOrId, out FileId))
            {
                file = context.SiteDb.Files.Get(FileId) as CmsFile;
            }

            long logid = -1;

            if (long.TryParse(NameOrId, out logid))
            {
                var logentry = context.SiteDb.Log.Get(logid);
                var repo = context.SiteDb.Images as IRepository;
                file = repo.GetByLog(logentry) as CmsFile;
            }

            if (file != null)
            {
                FileRenderer.RenderFile(context, file);
            }
        }

        public static void TextContentRender(FrontContext context, string NameOrId, Dictionary<string, string> Parameters)
        {

            throw new NotImplementedException();
        }

        public static void DefaultRender(FrontContext context, byte ConstType, string NameOrId, Dictionary<string, string> Parameters)
        {

            var modeltype = Service.ConstTypeService.GetModelType(ConstType);
            var repo = context.SiteDb.GetRepository(modeltype);

            ISiteObject siteobject = null;
            siteobject = repo?.GetByNameOrId(NameOrId) as ISiteObject;

            if (siteobject == null)
            {
                long logid = -1;

                if (long.TryParse(NameOrId, out logid))
                {
                    var logentry = context.SiteDb.Log.Get(logid);
                    siteobject = repo.GetByLog(logentry) as ISiteObject;
                }
            }

            if (siteobject != null)
            {
                var contenttype = Service.ConstTypeService.GetContentType(ConstType);
                context.RenderContext.Response.ContentType = contenttype;

                if (siteobject is ITextObject)
                {
                    context.RenderContext.Response.ContentType += ";charset=utf-8";
                    var textobject = siteobject as ITextObject;
                    context.RenderContext.Response.Body = DataConstants.DefaultEncoding.GetBytes(textobject.Body);
                }
                else if (siteobject is IBinaryFile)
                {
                    var binary = siteobject as IBinaryFile;
                    context.RenderContext.Response.Body = binary.ContentBytes;
                }
                else
                {
                    context.RenderContext.Response.ContentType += ";charset=utf-8";
                    var json = Lib.Helper.JsonHelper.Serialize(siteobject);
                    context.RenderContext.Response.Body = DataConstants.DefaultEncoding.GetBytes(json);
                }
            }
        }

        public static void KFileRender(FrontContext context, Dictionary<string, string> Parameters)
        {     
            if (!context.RenderContext.WebSite.EnableFileIOUrl)
            {
                context.RenderContext.Response.StatusCode = 503;
                context.RenderContext.Response.End = true;
            }

            string relative = context.RenderContext.Request.RelativeUrl;

            relative = Lib.Helper.StringHelper.ReplaceIgnoreCase(relative, "__kb/kfile/", "");

            var root = Kooboo.Data.AppSettings.GetFileIORoot(context.RenderContext.WebSite);

            string fullpath = Kooboo.Lib.Helper.IOHelper.CombinePath(root, relative);

            if (System.IO.File.Exists(fullpath))
            {
                string contentType = IOHelper.MimeType(relative);

                if (string.IsNullOrEmpty(contentType))
                {
                    contentType = "application/octet-stream";
                }
                context.RenderContext.Response.ContentType = contentType;
                var allbytes = Lib.Helper.IOHelper.ReadAllBytes(fullpath);
                context.RenderContext.Response.Body = allbytes; 
            }   
        }    
    }
}