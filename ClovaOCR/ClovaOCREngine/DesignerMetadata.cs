using System.Activities.Presentation.Metadata;
using System.ComponentModel;
using ClovaOCRActivities.Basic.OCR;

namespace ClovaOCRActivities
{
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            var builder = new AttributeTableBuilder();

            // Designers
//            var simpleClassifierDesigner = new DesignerAttribute(typeof(SimpleClassifierDesigner));
//            var simpleExtractorDesigner = new DesignerAttribute(typeof(SimpleExtractorDesigner));

            //Categories
//            var classifierCategoryAttribute = new CategoryAttribute("Synap Classifiers");
//            var extractorCategoryAttribute = new CategoryAttribute("Synap Extractors");
            var ocrCategoryAttribute = new CategoryAttribute("ClovaAI");

 //           builder.AddCustomAttributes(typeof(SimpleClassifier), classifierCategoryAttribute);
 //           builder.AddCustomAttributes(typeof(SimpleClassifier), simpleClassifierDesigner);

 //           builder.AddCustomAttributes(typeof(SimpleExtractor), extractorCategoryAttribute);
 //           builder.AddCustomAttributes(typeof(SimpleExtractor), simpleExtractorDesigner);

            builder.AddCustomAttributes(typeof(ClovaOCREngine), ocrCategoryAttribute);
            builder.AddCustomAttributes(typeof(ClovaOCREngine), nameof(ClovaOCREngine.Result), new CategoryAttribute("Output"));

            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}
