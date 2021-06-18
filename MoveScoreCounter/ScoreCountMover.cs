using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

[BepInPlugin("com.biendeo.vintage.scorecountmover", "Move Score Counter", "1.0.0")]
internal class ScoreCountMover : BaseUnityPlugin {
	private Camera hudCamera;
	private Transform hudTransform;
	private bool sceneChanged;
	private int windowId;
	private SpriteRenderer scorebgSpriteRenderer;
	private bool showWindow;
	private Vector3? lastPos;
	private readonly FileInfo configFile;

	public ScoreCountMover() {
		configFile = new FileInfo(Path.Combine(Paths.ConfigPath, "MoveScoreCounter.cfg"));
	}

	private void OnGUI() {
		if (hudCamera != null && hudTransform != null && scorebgSpriteRenderer && showWindow) {
			Vector3 min = scorebgSpriteRenderer.bounds.min;
			Vector3 vector = hudCamera.WorldToScreenPoint(scorebgSpriteRenderer.bounds.min);
			Vector3 vector2 = hudCamera.WorldToScreenPoint(scorebgSpriteRenderer.bounds.max);
			float y = (float)Screen.height - vector.y;
			float y2 = (float)Screen.height - vector2.y;
			vector2.y = y;
			vector.y = y2;
			Rect rect = GUI.Window(this.windowId, new Rect(vector, vector2 - vector), (int _) => {
				GUILayout.Box("BepInEx port by Biendeo", Array.Empty<GUILayoutOption>());
				GUI.DragWindow();
			}, "Score Counter Mover");
			float z = scorebgSpriteRenderer.bounds.min.z - hudCamera.transform.position.z;
			Vector3 position = new Vector3(rect.xMin, Screen.height - rect.yMax, z);
			Vector3 a = hudCamera.ScreenToWorldPoint(position);
			hudTransform.position += a - min;
			lastPos = new Vector3?(hudTransform.position);
		}
		if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F4) {
			showWindow = !showWindow;
			if (!showWindow) {
				WriteConfig();
			}
		}
	}

	private void Start() {
		SceneManager.activeSceneChanged += (Scene _, Scene __) => {
			sceneChanged = true;
		};
		windowId = UnityEngine.Random.Range(1024, int.MaxValue);
		try {
			using (BinaryReader binaryReader = new BinaryReader(configFile.Open(FileMode.Open, FileAccess.Read))) {
				lastPos = new Vector3?(new Vector3 {
					x = binaryReader.ReadSingle(),
					y = binaryReader.ReadSingle(),
					z = binaryReader.ReadSingle()
				});
			}
		} catch (Exception) { }
	}

	private void LateUpdate() {
		if (sceneChanged) {
			Scene activeScene = SceneManager.GetActiveScene();
			GameObject[] rootGameObjects = activeScene.GetRootGameObjects();
			if (activeScene.name == "Gameplay") {
				hudCamera = (from o in rootGameObjects
							 where o.name == "HudCamera"
							 select o.GetComponent<Camera>()).FirstOrDefault();
				hudTransform = (from o in rootGameObjects
								where o.name == "HUD"
								select o.transform).FirstOrDefault();
				scorebgSpriteRenderer = hudTransform.GetChild(0).GetChild(11).GetComponent<SpriteRenderer>();
				if (lastPos != null) {
					hudTransform.position = lastPos.Value;
				}
				return;
			}
			hudCamera = null;
			hudTransform = null;
			sceneChanged = false;
		}
	}

	private Task WriteConfig() {
		return Task.Run(() => {
			using (BinaryWriter binaryWriter = new BinaryWriter(configFile.Open(FileMode.Create, FileAccess.Write))) {
				if (lastPos != null) {
					binaryWriter.Write(lastPos.Value.x);
					binaryWriter.Write(lastPos.Value.y);
					binaryWriter.Write(lastPos.Value.z);
				}
			}
		});
	}
}
