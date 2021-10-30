using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using UiPath.OCR.Contracts;
using UiPath.OCR.Contracts.Activities;
using UiPath.OCR.Contracts.DataContracts;

namespace ClovaOCRActivities.Basic.OCR
{
    [DisplayName("Text PDF Engine")]
    public class TextPDFEngine : OCRCodeActivity
    {
        [Category("Input")]
        [Browsable(true)]
        public override InArgument<Image> Image { get => base.Image; set => base.Image = value; }


        [Category("Input")]
        [RequiredArgument]
        [Description("Pdf file path ")]
        public InArgument<string> PdfFilePath { get; set; }


        [Category("Output")]
        [Browsable(true)]
        public override OutArgument<string> Text { get => base.Text; set => base.Text = value; }


        /**
         * OCRENgine으로 동작하는데 필요한 함수 구현 
         * Dictionary<string,object> options에 필요한 값을 담아서 넘겨준다. 
         */
        public override Task<OCRResult> PerformOCRAsync(Image image, Dictionary<string, object> options, CancellationToken ct)
        {
            options["width"] = image.Width;
            options["height"] = image.Height;
            options["resolution"] = image.HorizontalResolution;
            var result =  OCRResultHelper.FromTextPdf(options);

            return result;
        }

        /**
         * Output 출력을 설정한다. PeformOCRAsync에서 options에 담겨진 값을 이용해서 최종 Output argument에 값을 설정한다. 
         */
        protected override void OnSuccess(CodeActivityContext context, OCRResult result)
        {

;       }
        //protected override void on

        protected override Dictionary<string, object> BeforeExecute(CodeActivityContext context)
        {
            return new Dictionary<string, object>
            {
                { "pdffilepath",  PdfFilePath.Get(context) }
            };
        }
    }
}
