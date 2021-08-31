using UnityEngine;
using System.Collections;

using UnityEngine.Networking;
//using System.Text.Json;
using AnnotationNameToImgRect = System.Collections.Generic.Dictionary<System.String, SerializableRect>;
using Newtonsoft.Json;

public class RestUploadingImg : MonoBehaviour
{
    public string screenShotURL= "http://192.168.0.128:5001/check_visuals_proxy";

    // Use this for initialization
    void Start()
    {
        
    }

    public void DoUploadFrameData(AnnotationNameToImgRect frameAnnotations)
    {
        StartCoroutine(internalUploadFrameData(frameAnnotations));
    }

    IEnumerator internalUploadFrameData(AnnotationNameToImgRect frameAnnotations)
    {
        // We should only read the screen after all rendering is complete
        yield return new WaitForEndOfFrame();

        // Create a texture the size of the screen, RGB24 format
        int width = Screen.width;
        int height = Screen.height;
        var tex = new Texture2D( width, height, TextureFormat.RGB24, false );

        // Read screen contents into the texture
        tex.ReadPixels( new Rect(0, 0, width, height), 0, 0 );
        tex.Apply();

        // Encode texture into PNG
        byte[] bytes = tex.EncodeToPNG();
        Destroy( tex );

        // Create a Web Form
        WWWForm form = new WWWForm();
        form.AddField("frameCount", Time.frameCount.ToString());
        form.AddBinaryData("fileUpload", bytes, "screenShot.png", "image/png");

        // Serialize the annotations to a JSON string 
        string annString = JsonConvert.SerializeObject(frameAnnotations);
        form.AddField("annotations", annString);

        // Upload to a cgi script
        using (var w = UnityWebRequest.Post(screenShotURL, form))
        {
            yield return w.SendWebRequest();
            if (w.result != UnityWebRequest.Result.Success) {
                print(w.error);
            }
            else {
                print("Finished Uploading Screenshot");
            }
        }
    }
}

