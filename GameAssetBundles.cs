using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;
using TMPro;
using Valve.VR;
using UnityEngine.Device;
using Technie.PhysicsCreator;

namespace NesSharp
{
    public class CartridgeObject : MonoBehaviour
    {
        public string romFileName = "";

        public void InsertCartridge()
        {
            if (Plugin.currentLoadedROM != romFileName)
            {
                Plugin.currentLoadedROM = romFileName;
                Plugin.loadSuccess = Plugin.console.LoadCartridge(romFileName);
            }

            text.GetComponent<Renderer>().enabled = false;
            gameObject.GetComponent<MeshRenderer>().enabled = false;

            Plugin.instance.UpdateConsole();
        }

        private void Start()
        {
            string nm = this.romFileName.Remove(this.romFileName.IndexOf(".nes"));
            nm = nm.Remove(0, nm.IndexOf("plugins/NES\\") + 12);

            if (nm == "")
            {
                nm = "UNKNOWN ROM";
            }

            text = Plugin.CreateTextWithoutDelete(nm, Color.white, gameObject.transform.position, -gameObject.transform.forward, 0.02f);

            //base.gameObject.transform.Find("GameTitle").GetComponent<TextMeshPro>().text = nm;
        }
        private bool vibRight;
        private bool vibLeft;
        private bool vibGrabRight;
        private bool vibGrabLeft;

        private bool insertCooldown;

        public GameObject text = null;

        public bool IsBeingUsed()
        {
            return Plugin.currentLoadedROM == romFileName;
        }
        public void Eject()
        {
            if (IsBeingUsed())
            {
                gameObject.GetComponent<MeshRenderer>().enabled = true;

                Plugin.console.Stop = true;
                Plugin.currentLoadedROM = "";

                gameObject.transform.position += Vector3.up * 0.2f;
                gameObject.transform.position += GameAssetBundles.NESObject.transform.forward * 0.35f;
                gameObject.transform.rotation = GameAssetBundles.NESObject.transform.rotation;

                GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(84, false, 0.2f);

                Plugin.loadSuccess = false;

                Plugin.screen.GetComponent<Renderer>().material.color = Color.black;

                text.GetComponent<Renderer>().enabled = true;

                Plugin.console.Cpu.Reset();
                Plugin.console.Ppu.Reset();
                Plugin.console.CpuMemory.Reset();
                Plugin.console.PpuMemory.Reset();

                Plugin.console = new Console();
            }
        }
        private void Update()
        {
            text.transform.position = gameObject.transform.position + Vector3.up * 0.2f;
            text.transform.forward = -gameObject.transform.forward;
            
            float distanceToRight = Vector3.Distance(gameObject.transform.position, GorillaTagger.Instance.offlineVRRig.rightHand.rigTarget.position);
            float distanceToLeft = Vector3.Distance(gameObject.transform.position, GorillaTagger.Instance.offlineVRRig.leftHand.rigTarget.position);

            //grab the console with right hand
            if (distanceToRight <= 0.16f && (!IsBeingUsed() || Plugin.heldCartridge == romFileName))
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

                        Plugin.holdinRight = true;

                        Plugin.holdingCartridge = true;
                        Plugin.heldCartridge = romFileName;

                        GorillaTagger.Instance.StartVibration(false, 0.4f, 0.25f);
                    }

                    gameObject.transform.position = GorillaTagger.Instance.offlineVRRig.rightHand.rigTarget.position;
                    gameObject.transform.rotation = Quaternion.Slerp(gameObject.transform.rotation, GorillaTagger.Instance.offlineVRRig.rightHand.rigTarget.rotation, 15f * Time.deltaTime);
                }
                else
                {
                    vibGrabRight = false;
                    Plugin.holdinRight = false;

                    if (Plugin.holdingCartridge && romFileName == Plugin.heldCartridge)
                    {
                        Plugin.heldCartridge = "";
                        Plugin.holdingCartridge = false;
                    }
                }
            }
            else { vibRight = false; }

            //grabbing left hand
            if (distanceToLeft <= 0.16f && (!IsBeingUsed() || Plugin.heldCartridge == romFileName))
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

                        Plugin.holdinLeft = true;

                        Plugin.holdingCartridge = true;
                        Plugin.heldCartridge = romFileName;

                        GorillaTagger.Instance.StartVibration(true, 0.4f, 0.25f);
                    }

                    gameObject.transform.position = GorillaTagger.Instance.offlineVRRig.leftHand.rigTarget.position;
                    gameObject.transform.rotation = Quaternion.Slerp(gameObject.transform.rotation, GorillaTagger.Instance.offlineVRRig.leftHand.rigTarget.rotation, 15f * Time.deltaTime);
                }
                else
                {
                    vibGrabLeft = false;
                    Plugin.holdinLeft = false;

                    if (Plugin.holdingCartridge && romFileName == Plugin.heldCartridge)
                    {
                        Plugin.heldCartridge = "";
                        Plugin.holdingCartridge = false;
                    }
                }
            }
            else { vibLeft = false; }

            if (vibGrabLeft && ControllerInputPoller.instance.leftControllerIndexFloat >= 0.6f || vibGrabRight && ControllerInputPoller.instance.rightControllerIndexFloat >= 0.6f) //trigger to insert
            {
                if (GameAssetBundles.NESObject != null)
                {
                    if (Vector3.Distance(gameObject.transform.position, GameAssetBundles.NESObject.transform.position) <= 0.65f)
                    {
                        if (!insertCooldown)
                        {
                            insertCooldown = true;

                            InsertCartridge();

                            GorillaTagger.Instance.StartVibration(vibGrabLeft, 0.3f, 0.2f);
                        }
                    }
                }
            }
            else
            {
                insertCooldown = false;
            }

            if (IsBeingUsed())
            {
                gameObject.transform.position = GameAssetBundles.NESObject.transform.position;
                if (SteamVR_Actions.gorillaTag_RightJoystickClick.GetLastState(SteamVR_Input_Sources.RightHand))
                {
                    Eject();
                }
            }
        }
    }
    
    //manages asset bundles
    internal class GameAssetBundles
    {
        public static AssetBundle AssetBundle { get; private set; }

        public static GameObject NESObject = null;
        public static GameObject CartridgePrefab = null;

        public static GameObject LoadAssetBundle(string assetName)
        {
            GameObject a = null;
            Debug.Log(Assembly.GetExecutingAssembly().GetManifestResourceNames()[0]);
            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("VoidNES.Resources.nes");
            if (s != null)
            {
                if (AssetBundle == null)
                {
                    AssetBundle = AssetBundle.LoadFromStream(s);
                }
                if (AssetBundle == null)
                {
                    Debug.Log("asset bundle was not found from stream");
                    return null;
                }
                if (AssetBundle.LoadAsset<GameObject>(assetName) == null)
                {
                    Debug.Log("asset bundle was not found from stream 2");
                    return null;
                }
                a = GameObject.Instantiate(AssetBundle.LoadAsset<GameObject>(assetName));

                if (a == null)
                {
                    Debug.Log("asset bundle gameobject instantiate was not created properly");
                    return null;
                }
            }
            else
            {
                Debug.Log("cant find path");
            }

            return a;
        }

        public static void Init()
        {
            if (NESObject == null)
                NESObject = LoadAssetBundle("NES");

            if (CartridgePrefab == null)
                CartridgePrefab = LoadAssetBundle("NESCartridge");
        }
    }
}
