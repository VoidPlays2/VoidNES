﻿using System;
using UnityEngine;
using System.Diagnostics;
using NesSharp.Mappers;
using Debug = UnityEngine.Debug;
using NesSharp;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;

/// <summary>
/// Represents a NES console.
/// </summary>
public class Console
{
    /// <summary>
    /// This Console's CPU.
    /// </summary>
    public readonly CPU Cpu;

    /// <summary>
    /// This Console's PPU
    /// </summary>
    public readonly PPU Ppu;

    /// <summary>
    /// This Console's CPU Memory.
    /// </summary>
    public readonly CPUMemory CpuMemory;

    /// <summary>
    /// This Console's PPU Memory.
    /// </summary>
    public readonly PPUMemory PpuMemory;

    /// <summary>
    /// This Console's Controller
    /// </summary>
    /// <remarks>
    /// This is currently set up to only work as controller 1.
    /// </remarks>
    public readonly Controller Controller;

    /// <summary>
    /// Gets or sets the console's Cartridge.
    /// </summary>
    /// <value>The Cartridge currently loaded in this console</value>
    public Cartridge Cartridge { get; private set; }

    /// <summary>
    /// Gets or sets the mapper for the cartridge currently loaded in this console.
    /// </summary>
    /// <value>The mapper for the cartridge currently loaded in this console.</value>
    public Mapper Mapper { get; private set; }

    /// <summary>
    /// Gets or sets the Action called when the Console is ready to draw a frame.
    /// </summary>
    /// <value>The Action called when the Console is ready to draw a frame.</value>
    public Action<byte[]> DrawAction { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this <see cref="T:Nescafe.Console"/> should stop.
    /// </summary>
    /// <value><c>true</c> if the console has been stopped; otherwise, <c>false</c>.</value>
    public bool Stop { get; set; }

    // Used internally to determine if we've reached a new frame
    bool _frameEvenOdd;

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Nescafe.Console"/> class.
    /// </summary>
    public Console()
    {
        Controller = new Controller();

        CpuMemory = new CPUMemory(this);
        PpuMemory = new PPUMemory(this);

        Cpu = new CPU(this);
        Ppu = new PPU(this);
    }

    /// <summary>
    /// Loads a cartridge into the console.
    /// </summary>
    /// <remarks>
    /// Logs information about the cartridge to stdout while loading including
    /// any errors that would cause the method to return <c>false</c>.
    /// </remarks>
    /// <returns><c>true</c>, if cartridge was loaded successfully, <c>false</c> otherwise.</returns>
    /// <param name="path">Path to the iNES cartridge file to load</param>
    public bool LoadCartridge(string path)
    {
        Debug.Log("Loading ROM " + path);

        Cartridge = new Cartridge(path);
        if (Cartridge.Invalid) return false;

        // Set mapper
        System.Console.Write("iNES Mapper Number: " + Cartridge.MapperNumber.ToString());
        switch (Cartridge.MapperNumber)
        {
            case 0:
                Debug.Log(" (NROM) Supported!");
                Mapper = new NromMapper(this);
                break;
            case 1:
                Debug.Log(" (MMC1) Supported!");
                Mapper = new Mmc1Mapper(this);
                break;
            case 2:
                Debug.Log(" (UxROM) Supported!");
                Mapper = new UxRomMapper(this);
                break;
            case 4:
                Debug.Log(" (MMC3) Supported!");
                Mapper = new Mmc3Mapper(this);
                break;
            default:
                Debug.Log(" mapper is not supported");
                return false;
        }

        Cpu.Reset();
        Ppu.Reset();

        CpuMemory.Reset();
        PpuMemory.Reset();

        _frameEvenOdd = false;
        return true;
    }

    /// <summary>
    /// Forces the console to call <see cref="T:Nescafe.Console.DrawAction"/>
    /// with current data from the PPU.
    /// </summary>
    public void DrawFrame()
    {
        DrawAction?.Invoke(Ppu.BitmapData);
        _frameEvenOdd = !_frameEvenOdd;
    }

    void GoUntilFrame()
    {
        bool orig = _frameEvenOdd;
        while (orig == _frameEvenOdd)
        {
            int cpuCycles = Cpu.Step();

            // 3 PPU cycles for each CPU cycle
            for (int i = 0; i < cpuCycles * 3; i++)
            {
                Ppu.Step();
                Mapper.Step();
            }
        }
    }

    /// <summary>
    /// Starts running the console and drawing frames.
    /// </summary>
    public async void Start()
    {
        /*Stop = false;
        byte[] bitmapData = Ppu.BitmapData;

        while (!Stop)
        {
            Stopwatch frameWatch = Stopwatch.StartNew();
            GoUntilFrame();
            frameWatch.Stop();

            long timeTaken = frameWatch.ElapsedMilliseconds;

            int sleepTime = (int)((1000.0 / 60) - timeTaken);
            Thread.Sleep(Math.Max(sleepTime, 0));
        }*/

        Stop = false;
        byte[] bitmapData = Ppu.BitmapData;

        while (!Stop)
        {
            Stopwatch frameWatch = Stopwatch.StartNew();
            GoUntilFrame();
            frameWatch.Stop();

            long timeTaken = frameWatch.ElapsedMilliseconds;
            int sleepTime = (int)((1000.0 / 60) - timeTaken);
            await Task.Delay(Math.Max(sleepTime, 0));
        }
    }
    public IEnumerator EmulationLoop()
    {        
        byte[] bitmapData = Ppu.BitmapData;

        WaitForEndOfFrame wait = new WaitForEndOfFrame();

        while (!Stop)
        {
            Stopwatch frameWatch = Stopwatch.StartNew();
            GoUntilFrame();
            frameWatch.Stop();

            long timeTaken = frameWatch.ElapsedMilliseconds;
            float sleepTime = (1000f / 60f) - timeTaken;

            if (sleepTime > 0)
                yield return new WaitForSeconds(sleepTime / 1000f);
            else
                yield return wait; // Just wait till end of frame to avoid overworking
        }
    }
}