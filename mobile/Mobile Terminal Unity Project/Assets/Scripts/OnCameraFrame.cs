﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vectrosity;
using Tango;
using System.Threading;
using DisruptorUnity3d;
using PlaynomicsPlugin;

public class OnCameraFrame : MonoBehaviour, ITangoVideoOverlay {

	TangoApplication tango;
	public UnityEngine.UI.Text textbox;
	public int frameNumber;
	public double timestamp;
	public Tango.TangoUnityImageData imgBuffer;
	public TangoPoseController poseData;
	public TangoARScreen offset;
	public FramePoolManager frameMgr;
	public BoundingBoxPoolManager boxMgr;
	private AnnotationsFetcher aFetcher_;
	private TangoPointCloud m_pointcloud;
	private ConcurrentQueue<Dictionary<int, FrameObjectData>> frameBuffer;
	private static ConcurrentQueue<BoxData> boundingBoxBufferToCalc;
	private static ConcurrentQueue<CreateBoxData> boundingBoxBufferToUpdate;
	//public RingBuffer<FrameObjectData> frameObjectBuffer;
	//public static RingBuffer<BoxData> boxBufferToCalc;
	//public static RingBuffer<CreateBoxData> boxBufferToUpdate;
	public List<Color> colors;
	Camera camForCalcThread;
	Thread calc;

	void Awake () {
		QualitySettings.vSyncCount = 0;  // VSync must be disabled
		Application.targetFrameRate = 30;
	}

	void Start()
	{
		tango = FindObjectOfType <TangoApplication> ();
		tango.Register (this);
		timestamp = gameObject.GetComponent<TangoARScreen> ().m_screenUpdateTime;
		poseData = gameObject.GetComponent<TangoPoseController> ();
		offset = gameObject.GetComponent<TangoARScreen> ();
		frameMgr = GameObject.FindObjectOfType<FramePoolManager>();
		frameNumber = 0;
		//frameObjects = new Dictionary<long, FrameObjectData> ();
		boxMgr = GameObject.FindObjectOfType<BoundingBoxPoolManager>();
		timestamp = 0;
		imgBuffer = null;
		m_pointcloud = PointCloudGUI.FindObjectOfType<TangoPointCloud>();
		frameBuffer = new ConcurrentQueue<Dictionary<int, FrameObjectData>> ();
		boundingBoxBufferToCalc = new ConcurrentQueue<BoxData> ();
		boundingBoxBufferToUpdate = new ConcurrentQueue<CreateBoxData> ();
//		frameObjectBuffer = new RingBuffer<FrameObjectData> (100000);
//		boxBufferToCalc = new RingBuffer<BoxData> (100000);
//		boxBufferToUpdate = new RingBuffer<CreateBoxData> (100000);
		camForCalcThread = GameObject.Find("Camera").GetComponent("Camera") as Camera;
		calc = new Thread (calculationsForBoundingBox);
		calc.Start ();

		colors = new List<Color> {
			new Color (255f/255, 109f/255, 124f/255),
			new Color (119f/255, 231f/255, 255f/255),
			new Color (82f/255, 255f/255, 127f/255),
			new Color (252f/255, 187f/255, 255f/255),
			new Color (255f/255, 193f/255, 130f/255)
		};
			

		// @Therese - these need to be moved somewhere to a higher-level entity as
		// configuration parameters (may be changed frequently during testing)
		string rootPrefix = "/icear/user";
		string userId = "peter"; // "mobile-terminal0";
		string serviceType = "object_recognizer";
		string serviceInstance = "yolo-mock"; // "yolo";

		NdnRtc.Initialize (rootPrefix, userId);

		string servicePrefix = rootPrefix + "/" + userId + "/" + serviceType;
		// AnnotationsFetcher instance might also be a singleton class
		// and initialized/created somewhere else. here just as an example
		aFetcher_ = new AnnotationsFetcher (servicePrefix, serviceInstance);

		// setup CNL logging 
		ILOG.J2CsMapping.Util.Logging.Logger.getLogger("").setLevel(ILOG.J2CsMapping.Util.Logging.Level.FINE);
		ILOG.J2CsMapping.Util.Logging.Logger.Write = delegate(string message) { Debug.Log (System.DateTime.Now + ": " + message); };
	}

	public void OnDestroy()
	{
		tango.Unregister(this);
	}


	void Update()
	{
		if (Input.touchCount == 1) {
			//spawnBox();
		}


		//int max = boxBufferToUpdate.Count;

//		for(int i = 0; i < max; i++) {
//			CreateBoxData temp;
//			bool success = boxBufferToUpdate.TryDequeue (out temp);
//			if (success) {
//				boxMgr.CreateBoundingBoxObject (temp.position, temp.x, temp.y, temp.z, temp.label, colors [Random.Range (0, colors.Count)]);
//			}
//		}

		int max = boundingBoxBufferToUpdate.Count;
		for(int i = 0; i < max; i++) {
			CreateBoxData temp = boundingBoxBufferToUpdate.Dequeue();
			Debug.Log("frame number for box: " + temp.frameNum);
			boxMgr.CreateBoundingBoxObject (temp.position, temp.x, temp.y, temp.z, temp.label, colors [Random.Range (0, colors.Count)]);
		}
			
	}

	public void OnTangoImageAvailableEventHandler(Tango.TangoEnums.TangoCameraId cameraId, Tango.TangoUnityImageData imageBuffer)
	{

		timestamp = offset.m_screenUpdateTime;
		imgBuffer = imageBuffer;



		int publishedFrameNo = NdnRtc.videoStream.processIncomingFrame (imageBuffer);

		if (publishedFrameNo >= 0) {
			frameMgr.CreateFrameObject (imgBuffer, publishedFrameNo, timestamp, poseData.finalPosition, poseData.finalRotation, offset.m_uOffset, offset.m_vOffset, camForCalcThread);
			//frameMgr.CreateFrameObject (imgBuffer, publishedFrameNo, timestamp, Vector3.zero, Quaternion.identity, offset.m_uOffset, offset.m_vOffset, camForCalcThread);

			//frameObjectBuffer.Enqueue (frameMgr.frameObjects [publishedFrameNo]);
			frameBuffer.Enqueue (frameMgr.frameObjects);
			//Debug.Log("frame number published: " + publishedFrameNo);
			// spawn fetching task for annotations of this frame
			// once successfully received, delegate callback will be called
			aFetcher_.fetchAnnotation (publishedFrameNo, delegate(string jsonArrayString) {
				string debuglog = jsonArrayString.Replace(System.Environment.NewLine, " ");
				Debug.Log("exception caught string: " + debuglog);
				Debug.Log("Received annotations JSON (frame " + publishedFrameNo + "): " + debuglog);
				//Debug.Log("annotations string length: " + jsonArrayString.Length);
				string[] testDebug = jsonArrayString.Split(']');
				string formatDebug = testDebug[0] + "]";
				try{
				Dictionary<int, FrameObjectData> frameObjects = frameBuffer.Dequeue();
				FrameObjectData temp;
				if(frameObjects.TryGetValue(publishedFrameNo, out temp))
				{

					//AnnotationData[] data = JsonHelper.FromJson<AnnotationData>(jsonArrayString);
						//try to print out how many characters the jsonArrayString has
					string format = "{ \"annotationData\": " + formatDebug + "}";
					AnnotationData data = JsonUtility.FromJson<AnnotationData>(format);
					for (int i = 0; i < data.annotationData.Length; i++)
					{
						if(data.annotationData[i].prob >= 0.7f)
						{
							Debug.Log("test: " + data.annotationData.Length);
							Debug.Log("test label: " + data.annotationData[i].label + " test xleft: " + data.annotationData[i].xleft
								+ " test xright: " + data.annotationData[i].xright + " test ytop: " + (1-data.annotationData[i].ytop) + " test ybottom: " + (1-data.annotationData[i].ybottom));
		//						Debug.Log("test xleft: " + data.annotationData[i].xleft);
		//						Debug.Log("test xright: " + data.annotationData[i].xright);
		//						Debug.Log("test ytop: " + data.annotationData[i].ytop);
		//						Debug.Log("test ybottom: " + data.annotationData[i].ybottom);
						}
					}
		//				FrameObjectData temp;
		//				bool success = frameObjectBuffer.TryDequeue(out temp);
		//				if(success)
		//				//FrameObjectData temp = frameBuffer.Dequeue();
		//				{
		//					Debug.Log("Frame info: " + publishedFrameNo);
		//					Debug.Log ("Frame info camera position: " + temp.camPos);
		//					Debug.Log ("Frame info camera rotation: " + temp.camRot);
		//					Debug.Log ("Frame info points number: " + temp.numPoints);
		//					Debug.Log ("Frame info points: " + temp.points.ToString());
		//				}


					Debug.Log("Frame number annotations: " + publishedFrameNo);
					Debug.Log ("Frame info camera position: " + temp.camPos);
					Debug.Log ("Frame info camera rotation: " + temp.camRot);
					//Debug.Log ("Frame info points number: " + temp.numPoints);
					Debug.Log ("Frame info points: " + temp.points.ToString());
						Debug.Log("test time difference: " + (Mathf.Abs((float)(temp.timestamp - offset.m_screenUpdateTime))) + " frame number: " + publishedFrameNo);

					//int boxCount = Mathf.Min(data.annotationData.Length, 2);
					int boxCount = data.annotationData.Length;

					BoxData annoData = new BoxData();
					Debug.Log("box created boxdata");
					annoData.frameNumber = publishedFrameNo;
					annoData.count = boxCount;
					annoData.points = temp.points;
					annoData.numPoints = temp.numPoints;
					annoData.cam = temp.cam;
					annoData.camPos = temp.camPos;
					annoData.camRot = temp.camRot;
					annoData.label = new string[boxCount];
					annoData.xleft = new float[boxCount];
					annoData.xright = new float[boxCount];
					annoData.ytop = new float[boxCount];
					annoData.ybottom = new float[boxCount];
					annoData.prob = new float[boxCount];

					for(int i = 0; i < boxCount; i++)
					{
						annoData.label[i] = data.annotationData[i].label;
						annoData.xleft[i] = data.annotationData[i].xleft;
						annoData.xright[i] = data.annotationData[i].xright;
						annoData.ytop[i] = 1-data.annotationData[i].ytop;
						annoData.ybottom[i] = 1-data.annotationData[i].ybottom;
						annoData.prob[i] = data.annotationData[i].prob;
					}

					Debug.Log("Received annotations box enqueue");
					//boxBufferToCalc.Enqueue(annoData);
					boundingBoxBufferToCalc.Enqueue(annoData);
				}
				else
				{
					//frame object was not in the pool, lifetime expired
					Debug.Log("Received annotations but frame expired");
				}
				}
				catch(System.Exception e)
				{
					Debug.Log("exception caught annotations: " + e);
					string debug = jsonArrayString.Replace(System.Environment.NewLine, " ");
					Debug.Log("exception caught string: " + debug);
					string format = "{ \"annotationData\": " + debug + "}";
					Debug.Log("exception caught string with format: " + format);
				}


			});

		} else {
			// frame was dropped by the encoder and was not published
		}


	}

	static void calculationsForBoundingBox()
	{
		while (true) {

			try
			{
			//Thread.Sleep (2);
//			BoxData temp;
//			Debug.Log ("box before dequeue");
//			bool success = boxBufferToCalc.TryDequeue (out temp);
//			Debug.Log ("box dequeue: " + success);
//			if (success) {
			if(boundingBoxBufferToCalc.Count > 0)
			{
				BoxData temp = boundingBoxBufferToCalc.Dequeue();
				int boxCount = temp.count;

				//Vector3[] min = new Vector3[boxCount];
				float[] averageZ = new float[boxCount];
				int[] numWithinBox = new int[boxCount];
				List<float>[] pointsInBounds = new List<float>[boxCount];

				for (int i = 0; i < boxCount; i++) {
					//min [i] = new Vector3 (100, 100, 100);
					pointsInBounds[i] = new List<float>();
					averageZ [i] = 0;
					numWithinBox [i] = 0;
				}

				Vector3[] points = temp.points;
				int count = temp.numPoints;

				temp.cam.transform.position = temp.camPos;
				temp.cam.transform.rotation = temp.camRot;

				Debug.Log ("Camera log: cam position" + temp.cam.transform.position.ToString());
				Debug.Log ("Camera log: frame cam position" + temp.camPos.ToString());
				Debug.Log ("Camera log: cam rotation" + temp.cam.transform.rotation.ToString());
				Debug.Log ("Camera log: frame cam rotation" + temp.camRot.ToString());

				Vector2[] centerPosXY = new Vector2[boxCount];
				Vector2[] worldCenter = new Vector2[boxCount];
				Vector3[] position = new Vector3[boxCount];

				Vector2[] viewportTopLeft = new Vector2[boxCount];
				Vector2[] viewportTopRight = new Vector2[boxCount];
				Vector2[] viewportBottomLeft = new Vector2[boxCount];
				Vector2[] viewportBottomRight = new Vector2[boxCount];

				Vector3[] worldTopLeft = new Vector3[boxCount];
				Vector3[] worldTopRight = new Vector3[boxCount];
				Vector3[] worldBottomLeft = new Vector3[boxCount];
				Vector3[] worldBottomRight = new Vector3[boxCount];

				float[] x = new float[boxCount];
				float[] y = new float[boxCount];
				float[] z = new float[boxCount];

				
				for (int i = 0; i < boxCount; i++) {


					//calucate 4 viewport corners
					viewportTopLeft [i] = new Vector2 (temp.xleft [i], temp.ytop [i]);
					viewportTopRight [i] = new Vector2 (temp.xright [i], temp.ytop [i]);
					viewportBottomLeft [i] = new Vector2 (temp.xleft [i], temp.ybottom [i]);
					viewportBottomRight [i] = new Vector2 (temp.xright [i], temp.ybottom [i]);


					//calculate center of box in viewport coords
					centerPosXY [i] = new Vector2 (temp.xleft [i] + Mathf.Abs (viewportTopLeft [i].x - viewportTopRight [i].x) / 2,
						temp.ybottom [i] + Mathf.Abs (viewportTopLeft [i].y - viewportBottomLeft [i].y) / 2);

				}

				//search points[]
				for (int i = 0; i < count; i++) {
					for (int j = 0; j < boxCount; j++) {
//						//calculate center of box in world coords
//						worldCenter [j] = temp.cam.ViewportToWorldPoint (new Vector2 (centerPosXY [j].x, centerPosXY [j].y));
//						//find point in points[] that most nearly matches center position
//						if (Vector2.Distance (new Vector2 (points [i].x, points [i].y), worldCenter [j]) < Vector2.Distance (new Vector2 (min [j].x, min [j].y), worldCenter [j])) {
//							min [j] = points [i];
//						}
						//find if points[i] is outside of the bounding box
						Vector3 viewportPoint = temp.cam.WorldToViewportPoint(points[i]);
						if (viewportPoint.x < temp.xleft[j] || viewportPoint.x > temp.xright[j] || viewportPoint.y < temp.ybottom[j] || viewportPoint.y > temp.ytop[j]) {
							//points[i] is out of the limits of the bounding box
						} else {
							//points[i] is in the bounding box
							pointsInBounds[j].Add(points[i].z);
							averageZ[j] += points[i].z;
							numWithinBox[j]++;
						}
					}
				}

				for (int i = 0; i < boxCount; i++) {
					float median;
					float depth;
					pointsInBounds [i].Sort ();
					//median = pointsInBounds [i][pointsInBounds[i].Count / 2];
						//Debug.Log("median = " + median);
					//averageZ [i] /= numWithinBox [i];

					if (!(pointsInBounds[i].Count == 0)) {
						//float depth = Mathf.Abs(min [i].z);
						median = pointsInBounds [i][pointsInBounds[i].Count / 2];
						depth = Mathf.Abs (median);
						//float depth = Mathf.Abs (averageZ [i]);
						if (depth < 0.5f)
							depth = 0.5f;
						
						//calculate center of box in world coords
						position [i] = temp.cam.ViewportToWorldPoint (new Vector3 (centerPosXY [i].x, centerPosXY [i].y, depth));

						Debug.Log ("box position: " + position.ToString ());
						//Debug.Log ("box: found min " + min.ToString ());

						//calculate Z value for world corners
						worldTopLeft [i] = temp.cam.ViewportToWorldPoint (new Vector3 (viewportTopLeft [i].x, viewportTopLeft [i].y, depth));
						worldTopRight [i] = temp.cam.ViewportToWorldPoint (new Vector3 (viewportTopRight [i].x, viewportTopRight [i].y, depth));
						worldBottomLeft [i] = temp.cam.ViewportToWorldPoint (new Vector3 (viewportBottomLeft [i].x, viewportBottomLeft [i].y, depth));
						worldBottomRight [i] = temp.cam.ViewportToWorldPoint (new Vector3 (viewportBottomRight [i].x, viewportBottomRight [i].y, depth));


						//calculate x, y, z size values
						x [i] = Mathf.Abs (Vector3.Distance (worldTopLeft [i], worldTopRight [i]));
						y [i] = Mathf.Abs (Vector3.Distance (worldTopLeft [i], worldBottomLeft [i]));
						z [i] = 0;

						if(temp.prob[i] >= 0.7f)
						{
							CreateBoxData boxData = new CreateBoxData ();
							boxData.label = temp.label [i];
							boxData.position = position [i];
							boxData.x = x [i];
							boxData.y = y [i];
							boxData.z = z [i];
							boxData.cam = temp.cam;
							boxData.frameNum = temp.frameNumber;
							//boxBufferToUpdate.Enqueue (boxData);
							boundingBoxBufferToUpdate.Enqueue (boxData);
						}
					}
				}
			}
		}
		catch(System.Exception e)
		{
				Debug.Log("exception caught" + e.ToString());
		}
	}
}
}

[System.Serializable]
public struct AnnotationData
{
	[System.Serializable]
	public struct ArrayEntry
	{
		public float xleft;
		public float xright;
		public float ytop;
		public float ybottom;
		public string label;
		public float prob;
	}

	public ArrayEntry[] annotationData;
}

public struct CreateBoxData
{
	public Vector3 position;
	public float x;
	public float y;
	public float z;
	public int frameNum;
	public string label;
	public Camera cam;
}

public struct BoxData
{
	public int frameNumber;
	public int count;
	public Vector3[] points;
	public int numPoints;
	public Camera cam;
	public Vector3 camPos;
	public Quaternion camRot;
	public float[] xleft;
	public float[] xright;
	public float[] ytop;
	public float[] ybottom;
	public float[] prob;
	public string[] label;
}
