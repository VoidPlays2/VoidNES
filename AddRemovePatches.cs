using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using Valve.VR;
using System.IO;
using System.Collections;
using PlayFab.DataModels;
using System.Threading;

namespace NesSharp
{
    [BepInPlugin("org.buddiesvoid.nesemulator.gorillatag", "NES Emulator", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Console console = new Console();

        public static GameObject screen;

        public static GameObject resetButton;

        private static GameObject[] fingerClicker = new GameObject[] { };

        public const string PATH_TO_ROM = "C:/Program Files (x86)/Steam/steamapps/common/Gorilla Tag/BepInEx/plugins/NES"; //common path to rom file

        public static string currentLoadedROM = "";

        public static bool holdinRight;
        public static bool holdinLeft;

        public static Plugin instance;


        public void OnEnable()
        {
            AddRemovePatches.AddPatches();
        }
        public void OnDisable()
        {
            AddRemovePatches.RemovePatches();
        }
        private static string[] fard;
        private IEnumerator StartInit() //because ts thing has a mind of its own!!
        {
            GameAssetBundles.Init();

            yield return new WaitForSeconds(2f);

            GameAssetBundles.Init();

            fingerClicker = new GameObject[2]; //this creates little clickers on your hand. This was gonna be for interacting with the console but for now its scrapped.

            Transform[] hands = new Transform[2]
            {
                GorillaTagger.Instance.rightHandTransform,
                GorillaTagger.Instance.leftHandTransform
            };

            for (int i = 0; i < 2; i++)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                GameObject.Destroy(sphere.GetComponent<SphereCollider>());
                sphere.GetComponent<Renderer>().enabled = false;

                sphere.transform.localScale = Vector3.one * 0.01f;
                sphere.transform.SetParent(hands[i], false);
                sphere.transform.localPosition = new Vector3(0f, -0.1f, 0f);

                fingerClicker[i] = sphere;
            }

            try
            {
                Vector3 ohYeah = GorillaLocomotion.GTPlayer.Instance.bodyCollider.transform.position + GorillaLocomotion.GTPlayer.Instance.bodyCollider.transform.forward * 0.7f;

                for (int i = 0; i < fard.Length; i++)
                {
                    CreateCartridge(fard[i], ohYeah);

                    ohYeah += Vector3.forward * 0.28f;
                }
            } catch (Exception e)
            {
                Debug.LogError("wtf cant create things --- " + e.ToString());
            }
        }
        public static bool loadSuccess;
        public static bool rstAntiRepeat;
        public static float rstCooldown;
        public static GameObject CreateTextWithoutDelete(string text, Color color, Vector3 position, Vector3 rotation, float scale)
        {
            GameObject textObj = new GameObject("Textthingy");

            TextMesh tm = textObj.AddComponent<TextMesh>();

            tm.text = text;
            tm.color = color;
            tm.fontSize = 10;

            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;

            textObj.transform.position = position;
            textObj.transform.forward = rotation;
            textObj.transform.localScale *= scale;

            return textObj;
        }
        public static void CreateCartridge(string romName, Vector3 position)
        {
            if (GameAssetBundles.CartridgePrefab == null)
                GameAssetBundles.LoadAssetBundle("NESCartridge");

            GameObject cart = UnityEngine.Object.Instantiate(GameAssetBundles.CartridgePrefab, position, Quaternion.identity);

            cart.GetComponent<Renderer>().material.shader = Shader.Find("Universal Render Pipeline/Lit");

            cart.GetComponent<Renderer>().material.color = new Color(0.2f, 0.2f, 0.2f);

            cart.AddComponent<CartridgeObject>().romFileName = romName;

            Debug.Log("Created cartridge asset for game " + romName);
        }
        private static float consoleRetryCount = 5;
        public void Start()
        {
            if (instance == null)
                instance = this;
            
            try
            {
                StartCoroutine(StartInit());
            }
            catch (Exception ex)
            {
                Debug.LogError("Error initializing: " + ex);
            }

            if (!Directory.Exists(PATH_TO_ROM))
                Directory.CreateDirectory(PATH_TO_ROM);

            string[] readFiles = Directory.GetFiles(PATH_TO_ROM);

            fard = readFiles;

            console.DrawAction = HandleFrame;

            CreateScreen();
        }
        private bool hasInitTheTHing;

        private void CreateScreen()
        {
            if (screen == null)
            {
                screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
                screen.name = "NES Screen";
                screen.transform.localScale = new Vector3(256f / 240f, 1, 1);

                screen.GetComponent<Renderer>().material.color = Color.black;

                //screen.transform.localScale = new Vector3(1.5f, 1.5f, 0f);
                UnityEngine.Object.Destroy(screen.GetComponent<MeshCollider>());
            }
        }
        public void UpdateConsole()
        {
            StartCoroutine(console.EmulationLoop());
        }
        private bool vibRight;
        private bool vibLeft;

        private bool vibGrabRight;
        private bool vibGrabLeft;

        public static bool holdingCartridge;
        public static string heldCartridge = "";
        public void Update()
        {
            if (console.Stop)
                screen.GetComponent<Renderer>().material.color = Color.black;

            //controller input
            if (loadSuccess && !console.Stop && (GameAssetBundles.NESObject != null && Vector3.Distance(GameAssetBundles.NESObject.transform.position, GorillaTagger.Instance.offlineVRRig.transform.position) <= 12f))
            {
                console.Controller.setButtonState(Controller.Button.A, InputLib.A() || Keyboard.current.zKey.isPressed);
                console.Controller.setButtonState(Controller.Button.B, InputLib.B() || Keyboard.current.xKey.isPressed);

                console.Controller.setButtonState(Controller.Button.Left, InputLib.LeftJoystick().x <= -0.4f || Keyboard.current.leftArrowKey.isPressed);
                console.Controller.setButtonState(Controller.Button.Right, InputLib.LeftJoystick().x >= 0.4f || Keyboard.current.rightArrowKey.isPressed);

                console.Controller.setButtonState(Controller.Button.Down, InputLib.LeftJoystick().y <= -0.4f || Keyboard.current.downArrowKey.isPressed);
                console.Controller.setButtonState(Controller.Button.Up, InputLib.LeftJoystick().y >= 0.4f || Keyboard.current.upArrowKey.isPressed);

                console.Controller.setButtonState(Controller.Button.Start, InputLib.X() || Keyboard.current.qKey.isPressed);
                console.Controller.setButtonState(Controller.Button.Select, InputLib.Y() || Keyboard.current.wKey.isPressed);
            } else
            {
                console.Controller.setButtonState(Controller.Button.A, false);
                console.Controller.setButtonState(Controller.Button.B, false);

                console.Controller.setButtonState(Controller.Button.Left, false);
                console.Controller.setButtonState(Controller.Button.Right, false);

                console.Controller.setButtonState(Controller.Button.Down, false);
                console.Controller.setButtonState(Controller.Button.Up, false);

                console.Controller.setButtonState(Controller.Button.Start, false);
                console.Controller.setButtonState(Controller.Button.Select, false);
            }

            if (GameAssetBundles.NESObject != null)
            {
                if (!hasInitTheTHing)
                {
                    hasInitTheTHing = true;

                    foreach (Renderer diddy in GameAssetBundles.NESObject.GetComponentsInChildren<Renderer>())
                    {
                        diddy.material.shader = Shader.Find("Universal Render Pipeline/Lit");
                    }

                    GameAssetBundles.NESObject.transform.localScale *= 2f;

                    GameAssetBundles.NESObject.transform.position = GorillaLocomotion.GTPlayer.Instance.bodyCollider.transform.position + GorillaLocomotion.GTPlayer.Instance.bodyCollider.transform.forward * 1.5f;
                    GameAssetBundles.NESObject.transform.rotation = GorillaLocomotion.GTPlayer.Instance.bodyCollider.transform.rotation;

                    GameAssetBundles.NESObject.transform.Rotate(0, 180, 0);

                    Debug.Log("Console loaded!");
                }
                float distanceToRight = Vector3.Distance(GameAssetBundles.NESObject.transform.position, GorillaTagger.Instance.offlineVRRig.rightHand.rigTarget.position);
                float distanceToLeft = Vector3.Distance(GameAssetBundles.NESObject.transform.position, GorillaTagger.Instance.offlineVRRig.leftHand.rigTarget.position);

                //grab the console with right hand
                if (distanceToRight <= 0.5f && heldCartridge == "")
                {
                    if (!vibRight)
                    {
                        vibRight = true;
                        GorillaTagger.Instance.StartVibration(false, 0.25f, 0.1f);
                    }

                    if (ControllerInputPoller.instance.rightGrab)
                    {
                        if (!vibGrabRight)
                        {
                            vibGrabRight = true;

                            holdinRight = true;

                            GorillaTagger.Instance.StartVibration(false, 0.4f, 0.25f);
                        }

                        GameAssetBundles.NESObject.transform.position = GorillaTagger.Instance.offlineVRRig.rightHand.rigTarget.position;
                        GameAssetBundles.NESObject.transform.rotation = Quaternion.Slerp(GameAssetBundles.NESObject.transform.rotation, GorillaTagger.Instance.offlineVRRig.rightHand.rigTarget.rotation, 15f * Time.deltaTime);
                    }
                    else
                    {
                        vibGrabRight = false;
                        holdinRight = false;
                    }
                }
                else { vibRight = false; }

                //grabbing left hand
                if (distanceToLeft <= 0.5f && heldCartridge == "")
                {
                    if (!vibLeft)
                    {
                        vibLeft = true;
                        GorillaTagger.Instance.StartVibration(true, 0.25f, 0.1f);
                    }

                    if (ControllerInputPoller.instance.leftGrab)
                    {
                        if (!vibGrabLeft)
                        {
                            vibGrabLeft = true;

                            holdinLeft = true;

                            GorillaTagger.Instance.StartVibration(true, 0.4f, 0.25f);
                        }

                        GameAssetBundles.NESObject.transform.position = GorillaTagger.Instance.offlineVRRig.leftHand.rigTarget.position;
                        GameAssetBundles.NESObject.transform.rotation = Quaternion.Slerp(GameAssetBundles.NESObject.transform.rotation, GorillaTagger.Instance.offlineVRRig.leftHand.rigTarget.rotation, 15f * Time.deltaTime);
                    }
                    else
                    {
                        vibGrabLeft = false;
                        holdinLeft = false;
                    }
                }
                else { vibLeft = false; }

                /*if (fingerClicker != null)
                {
                    if (!vibGrabLeft && !vibGrabRight && fingerClicker.Length == 2)
                    {
                        distanceToRight = Vector3.Distance(resetButton.transform.position, fingerClicker[0].transform.position);
                        distanceToLeft = Vector3.Distance(resetButton.transform.position, fingerClicker[1].transform.position);

                        if (distanceToRight <= 0.11f)
                        {
                            if (!rstAntiRepeat)
                            {
                                rstAntiRepeat = true;

                                GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(67, false, 0.08f);

                                if (Time.time > rstCooldown)
                                {
                                    rstCooldown = Time.time + 1.5f;

                                    console.Cpu.Reset();
                                    console.Ppu.Reset();
                                }
                            }
                        }
                        else
                        {
                            if (distanceToLeft <= 0.11f)
                            {
                                if (!rstAntiRepeat)
                                {
                                    rstAntiRepeat = true;

                                    GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(67, true, 0.08f);

                                    if (Time.time > rstCooldown)
                                    {
                                        rstCooldown = Time.time + 1.5f;

                                        console.Cpu.Reset();
                                        console.Ppu.Reset();
                                    }
                                }
                            }
                            else
                            {
                                rstAntiRepeat = false;
                            }
                        }
                    }
                }
            }*/

                if (screen != null && GameAssetBundles.NESObject != null)
                {
                    screen.transform.position = GameAssetBundles.NESObject.transform.position + GameAssetBundles.NESObject.transform.forward * -1.2f + Vector3.up * 0.8f;
                    screen.transform.rotation = GameAssetBundles.NESObject.transform.rotation;
                    screen.transform.Rotate(0, 180, 180);
                }
                else if (screen == null)
                    CreateScreen();

                if (Vector3.Distance(GameAssetBundles.NESObject.transform.position, Vector3.zero) <= 5f && hasInitTheTHing)
                {
                    foreach (Renderer diddy in GameAssetBundles.NESObject.GetComponentsInChildren<Renderer>())
                    {
                        diddy.material.shader = Shader.Find("Universal Render Pipeline/Lit");
                    }

                    GameAssetBundles.NESObject.transform.localScale = Vector3.one * 2f;

                    GameAssetBundles.NESObject.transform.position = GorillaLocomotion.GTPlayer.Instance.bodyCollider.transform.position + GorillaLocomotion.GTPlayer.Instance.bodyCollider.transform.forward * 1.5f;
                    GameAssetBundles.NESObject.transform.rotation = GorillaLocomotion.GTPlayer.Instance.bodyCollider.transform.rotation;

                    GameAssetBundles.NESObject.transform.Rotate(0, 180, 0);
                }
            }
            else //bruh
            {
                if (Time.time > consoleRetryCount)
                {
                    consoleRetryCount = Time.time + 2.5f;

                    Debug.Log("reloading console...");

                    GameAssetBundles.Init();
                }
                
            }
        }

        public void HandleFrame(byte[] data)
        {
            if (currentLoadedROM == "" || screen == null || data.Length != 256 * 240)
            {
                Debug.LogError($"Failed to render: invalid state or data length: data valid? {data.Length == 256 * 240}, loaded ROM: {currentLoadedROM}, screen valid? {screen != null}");
                return;
            }

            Color32[] nesPalette = new Color32[64]
            {
        new Color32(124,124,124,255), new Color32(0,0,252,255), new Color32(0,0,188,255), new Color32(68,40,188,255),
        new Color32(148,0,132,255), new Color32(168,0,32,255), new Color32(168,16,0,255), new Color32(136,20,0,255),
        new Color32(80,48,0,255), new Color32(0,120,0,255), new Color32(0,104,0,255), new Color32(0,88,0,255),
        new Color32(0,64,88,255), new Color32(0,0,0,255), new Color32(0,0,0,255), new Color32(0,0,0,255),
        new Color32(188,188,188,255), new Color32(0,120,248,255), new Color32(0,88,248,255), new Color32(104,68,252,255),
        new Color32(216,0,204,255), new Color32(228,0,88,255), new Color32(248,56,0,255), new Color32(228,92,16,255),
        new Color32(172,124,0,255), new Color32(0,184,0,255), new Color32(0,168,0,255), new Color32(0,168,68,255),
        new Color32(0,136,136,255), new Color32(0,0,0,255), new Color32(0,0,0,255), new Color32(0,0,0,255),
        new Color32(248,248,248,255), new Color32(60,188,252,255), new Color32(104,136,252,255), new Color32(152,120,248,255),
        new Color32(248,120,248,255), new Color32(248,88,152,255), new Color32(248,120,88,255), new Color32(252,160,68,255),
        new Color32(248,184,0,255), new Color32(184,248,24,255), new Color32(88,216,84,255), new Color32(88,248,152,255),
        new Color32(0,232,216,255), new Color32(120,120,120,255), new Color32(0,0,0,255), new Color32(0,0,0,255),
        new Color32(252,252,252,255), new Color32(164,228,252,255), new Color32(184,184,248,255), new Color32(216,184,248,255),
        new Color32(248,184,248,255), new Color32(248,164,192,255), new Color32(240,208,176,255), new Color32(252,224,168,255),
        new Color32(248,216,120,255), new Color32(216,248,120,255), new Color32(184,248,184,255), new Color32(184,248,216,255),
        new Color32(0,252,252,255), new Color32(248,216,248,255), new Color32(0,0,0,255), new Color32(0,0,0,255)
            };

            Color32[] pixels = new Color32[256 * 240];
            for (int i = 0; i < data.Length; i++)
            {
                byte index = data[i];
                pixels[i] = index < 64 ? nesPalette[index] : (Color32)Color.magenta;
            }

            Color32[] flippedPixels = new Color32[256 * 240];
            for (int y = 0; y < 240; y++)
            {
                for (int x = 0; x < 256; x++)
                {
                    flippedPixels[y * 256 + x] = pixels[y * 256 + (255 - x)];
                }
            }

            Texture2D t = new Texture2D(256, 240, TextureFormat.RGBA32, false);
            t.SetPixels32(flippedPixels);
            t.Apply();

            Material material = screen.GetComponent<Renderer>().material;
            material.shader = Shader.Find("Unlit/Texture");
            material.mainTexture = t;

            screen.transform.localScale = new Vector3(256f / 240f, 1, 1);
        }




        public class AddRemovePatches
        {
            private static Harmony Instance = null;

            public static void AddPatches()
            {
                if (Instance == null)
                {
                    Instance = new Harmony("org.buddiesvoid.nesemulator.gorillatag");
                    Instance.PatchAll(Assembly.GetExecutingAssembly());
                }
            }

            public static void RemovePatches()
            {
                if (Instance != null)
                {
                    Instance.UnpatchSelf();
                    Instance = null;
                }
            }
        }

        public class InputLib
        {
            public static ControllerInputPoller Inst() { return ControllerInputPoller.instance; }
            public static bool RT() { return Inst().rightControllerIndexFloat > 0.6f; }
            public static bool LT() { return Inst().leftControllerIndexFloat > 0.6f; }
            public static bool RG() { return Inst().rightControllerGripFloat > 0.6f; }
            public static bool LG() { return Inst().leftControllerGripFloat > 0.6f; }
            public static bool A() { return Inst().rightControllerPrimaryButton; }
            public static bool B() { return Inst().rightControllerSecondaryButton; }
            public static bool X() { return Inst().leftControllerPrimaryButton; }
            public static bool Y() { return Inst().leftControllerSecondaryButton; }

            public static Vector2 LeftJoystick() { return SteamVR_Actions.gorillaTag_LeftJoystick2DAxis.axis; }
        }
    }
}
