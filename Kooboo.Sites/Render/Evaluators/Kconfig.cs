//Copyright (c) 2018 Yardi Technology Limited. Http://www.kooboo.com 
//All rights reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kooboo.Dom;
using Kooboo.Sites.Render.RenderTask;

namespace Kooboo.Sites.Render.Evaluators
{
                                           
    public class KConfigContentEvaluator : IEvaluator
    {
        public EvaluatorResponse Evaluate(Node node, EvaluatorOption options)
        {
            if (options.IgnoreEvaluators.HasFlag(EnumEvaluator.kConfig))
            {
                return null;
            }

            if (node.nodeType != enumNodeType.ELEMENT)
            {
                return null;
            }
            var element = node as Element;

            string attName = null;
            string attValue = null; 
            foreach (var item in element.attributes)
            {
                if (item.name == "k-config")
                {
                    attName = item.name;
                    attValue = item.value; 
                    break;
                }
            }
            if (!string.IsNullOrEmpty(attName))
            {
                var response = new EvaluatorResponse();
                List<IRenderTask> result = new List<IRenderTask>();

                result.Add(new KConfigRenderTask(element, attValue));  
                response.ContentTask = result;
                response.OmitTag = true; 
                      
                return response;
            }
            return null;
        }
                  
    }

}
