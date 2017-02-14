using System;
using System.Diagnostics;
using System.Linq;
using AVFoundation;
using CoreFoundation;
using CoreGraphics;
using CoreMedia;
using CoreVideo;
using Foundation;
using UIKit;

namespace ARDemo
{
	public class VideoCamera
	{
		AVCaptureSession session;

		AVCaptureDevice device;
		AVCaptureDeviceInput input;

		AVCaptureVideoDataOutput output;
		DispatchQueue queue;

		public Action<int, int, int, IntPtr> CapturedFrame;
	    private OutputRecorder outputRecorder;

	    public VideoCamera ()
		{
			CreateSession ();
			CreateDevice ();
			CreateInput ();
			CreateOutput ();
		}

		public void Start ()
		{
			session.StartRunning ();
		}

		public void Stop ()
		{
			session.StopRunning ();
		}

		public float FieldOfView {
			get {
				return device.ActiveFormat.VideoFieldOfView;
			}
		}

		void CreateSession ()
		{
			session = new AVCaptureSession ();
			session.SessionPreset = AVCaptureSession.PresetMedium;
		}

		void CreateDevice ()
		{
            device = AVCaptureDevice.DefaultDeviceWithMediaType(AVMediaType.Video);
            if (device == null)
            {
                Console.WriteLine("No captureDevice - this won't work on the simulator, try a physical device");
               
            }
            //Configure for 15 FPS. Note use of LockForConigfuration()/UnlockForConfiguration()
            NSError error;
            device.LockForConfiguration(out error);
            if (error != null)
            {
                Console.WriteLine(error);
                device.UnlockForConfiguration();
            }
            if (UIDevice.CurrentDevice.CheckSystemVersion(7, 0))
                device.ActiveVideoMinFrameDuration = new CMTime(1, 15);
            device.UnlockForConfiguration();

            

   //         NSError error;

			//device = AVCaptureDevice.GetDefaultDevice(AVMediaType.Video);
			//if (device == null) {
			//	throw new Exception ("No default video device");
			//}

			//device.LockForConfiguration(out error);
			//if (error != null) {
			//	throw new Exception ("Could not configure. Error: " + error);
			//}

			//device.ActiveVideoMinFrameDuration = new CMTime (1, 30);

			//device.UnlockForConfiguration();
		}

		void CreateInput ()
		{
            var input = AVCaptureDeviceInput.FromDevice(device);
            if (input == null)
            {
                Console.WriteLine("No input - this won't work on the simulator, try a physical device");
              //  return false;
            }
            session.AddInput(input);

   //         NSError error;

			//input = AVCaptureDeviceInput.FromDevice (device, out error);
			//if (input == null) {
			//	throw new Exception ("Could not capture from " + device + " Error: " + error);
			//}

			//session.AddInput (input);
		}


		void CreateOutput ()
		{
            var settings = new CVPixelBufferAttributes
            {
                PixelFormatType = CVPixelFormatType.CV32BGRA
            };
		    output = new AVCaptureVideoDataOutput {WeakVideoSettings = settings.Dictionary};
            
                queue = new DispatchQueue("myQueue");
                outputRecorder = new OutputRecorder(this);
             //   outputRecorder.SampleCallback += NewImageHandler;
                output.SetSampleBufferDelegate(outputRecorder, queue);
                session.AddOutput(output);

            //output = new AVCaptureVideoDataOutput ();
            //         var settings = new CVPixelBufferAttributes
            //         {
            //             PixelFormatType = CVPixelFormatType.CV32BGRA
            //         };
            //         output.WeakVideoSettings = settings.Dictionary;
            //         //output.VideoSettings = new AVVideoSettings (CVPixelFormatType.CV32BGRA);

            //queue = new DispatchQueue ("VideoCameraQueue");
            //output.SetSampleBufferDelegate(new VideoCameraDelegate { Camera = this }, queue);
            //session.AddOutput (output);
        }

		public event Action<UIImage> FrameCaptured = delegate {};

		void OnFrameCaptured (UIImage frame)
		{
			DispatchQueue.MainQueue.DispatchAsync (() => FrameCaptured (frame));
		}

		class VideoCameraDelegate : AVCaptureVideoDataOutputSampleBufferDelegate
		{
			public VideoCamera Camera;
			public override void DidOutputSampleBuffer (AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, 
				                                        AVCaptureConnection connection)
			{
				try {
					var frame = ImageFromSampleBuffer (sampleBuffer);
					Camera.OnFrameCaptured (frame);
					sampleBuffer.Dispose ();
				} catch (Exception ex) {
					Debug.WriteLine (ex);
				}
			}

			static UIImage ImageFromSampleBuffer (CMSampleBuffer sampleBuffer)
			{
				using (var pixelBuffer = sampleBuffer.GetImageBuffer () as CVPixelBuffer){
					pixelBuffer.Lock (CVOptionFlags.None);
					var baseAddress = pixelBuffer.BaseAddress;
					int bytesPerRow = (int)pixelBuffer.BytesPerRow;
					int width = (int)pixelBuffer.Width;
					int height = (int)pixelBuffer.Height;
					var flags = CGBitmapFlags.PremultipliedFirst | CGBitmapFlags.ByteOrder32Little;
					using (var cs = CGColorSpace.CreateDeviceRGB ())
					using (var context = new CGBitmapContext (baseAddress,width, height, 8, bytesPerRow, cs, (CGImageAlphaInfo) flags))
					using (var cgImage = context.ToImage ()){
						pixelBuffer.Unlock (CVOptionFlags.None);
						return UIImage.FromImage (cgImage);
					}
				}
			}
		}

        public class OutputRecorder : AVCaptureVideoDataOutputSampleBufferDelegate
        {
            private readonly VideoCamera videoCamera;
            public EventHandler SampleCallback { get; set; }
            public UIImage LastFrame { get; set; }

            public OutputRecorder(VideoCamera videoCamera)
            {
                this.videoCamera = videoCamera;
            }

            public override void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
            {
                try
                {
                    LastFrame = ImageFromSampleBuffer(sampleBuffer);
                    videoCamera.OnFrameCaptured(LastFrame);
                    SampleCallback.Invoke(this, System.EventArgs.Empty);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    sampleBuffer.Dispose();
                }
            }

            private UIImage ImageFromSampleBuffer(CMSampleBuffer sampleBuffer)
            {
                // Get the CoreVideo image
                using (var pixelBuffer = sampleBuffer.GetImageBuffer() as CVPixelBuffer)
                {
                    if (pixelBuffer == null) throw new ArgumentNullException("pixelBuffer");
                    // Lock the base address
                    pixelBuffer.Lock(CVPixelBufferLock.None);
                    // Get the number of bytes per row for the pixel buffer
                    var baseAddress = pixelBuffer.BaseAddress;
                    int bytesPerRow = (int)pixelBuffer.BytesPerRow;
                    int width = (int)pixelBuffer.Width;
                    int height = (int)pixelBuffer.Height;
                    var flags = CGBitmapFlags.PremultipliedFirst | CGBitmapFlags.ByteOrder32Little;
                    // Create a CGImage on the RGB colorspace from the configured parameter above
                    using (var cs = CGColorSpace.CreateDeviceRGB())
                    {
                        using (var context = new CGBitmapContext(baseAddress, width, height, 8, bytesPerRow, cs, (CGImageAlphaInfo)flags))
                        {
                            using (CGImage cgImage = context.ToImage())
                            {
                                pixelBuffer.Unlock(CVPixelBufferLock.None);
                                //                                return UIImage.FromImage (cgImage,(nfloat)1.0,UIImageOrientation.Right);

                                var img = UIImage.FromImage(cgImage);
                                UIGraphics.BeginImageContextWithOptions(img.Size, false, img.CurrentScale);
                                var ctxt = UIGraphics.GetCurrentContext();
                                ctxt.TranslateCTM((nfloat)(.5 * img.Size.Width), (nfloat)(.5 * img.Size.Height));

                                var orientation = UIDevice.CurrentDevice.Orientation;
                                if (orientation.Equals(UIDeviceOrientation.Portrait))
                                    ctxt.RotateCTM(((nfloat)Math.PI / 2));
                                else if (orientation.Equals(UIDeviceOrientation.PortraitUpsideDown))
                                    ctxt.RotateCTM(((nfloat)(3 * Math.PI / 2)));
                                else if (orientation.Equals(UIDeviceOrientation.LandscapeRight))
                                    ctxt.RotateCTM(((nfloat)(Math.PI)));

                                img.Draw(new CGRect(-.5 * img.Size.Width, -.5 * img.Size.Height, img.Size.Width, img.Size.Height));
                                var normalizedImage = UIGraphics.GetImageFromCurrentImageContext();
                                UIGraphics.EndImageContext();

                                return normalizedImage;
                            }
                        }
                    }
                }
            }
        }
    }

}