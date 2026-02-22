using UnityEngine;
using Mirror;
using Steamworks;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(AudioSource))]
public class SteamVoiceChat : NetworkBehaviour
{
    private AudioSource audioSource;
    private uint optimalSampleRate;

    // Черга для зберігання розпакованих аудіоданих перед відтворенням
    private Queue<float> audioBuffer = new Queue<float>();

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Починаємо програвати порожній звук (далі ми будемо "підкидати" туди голос)
        if (!isLocalPlayer)
        {
            audioSource.Play();
        }

        // Якщо це наш гравець - вмикаємо мікрофон через Steam
        if (isLocalPlayer)
        {
            optimalSampleRate = SteamUser.GetVoiceOptimalSampleRate();
            SteamUser.StartVoiceRecording();
        }
    }

    void OnDestroy()
    {
        if (isLocalPlayer)
        {
            SteamUser.StopVoiceRecording();
        }
    }

    void Update()
    {
        // Тільки локальний гравець записує і відправляє свій голос
        if (!isLocalPlayer) return;

        uint compressedSize;

        // Виправлено: GetAvailableVoice тепер приймає лише 1 аргумент (out compressedSize)
        if (SteamUser.GetAvailableVoice(out compressedSize) == EVoiceResult.k_EVoiceResultOK && compressedSize > 0)
        {
            byte[] compressedVoice = new byte[compressedSize];
            uint bytesWritten;

            // Виправлено: GetVoice тепер приймає 4 аргументи
            if (SteamUser.GetVoice(true, compressedVoice, compressedSize, out bytesWritten) == EVoiceResult.k_EVoiceResultOK)
            {
                // Відправляємо на сервер
                CmdSendVoice(compressedVoice);
            }
        }
    }

    // Відправка на сервер. Channel = 1 - це зазвичай Unreliable канал (без гарантії доставки),
    // що ідеально для голосу, щоб не було затримок.
    [Command(channel = 1)]
    void CmdSendVoice(byte[] compressedVoice)
    {
        // Сервер розсилає всім КРІМ того, хто говорить (includeOwner = false)
        RpcPlayVoice(compressedVoice);
    }

    [ClientRpc(channel = 1, includeOwner = false)]
    void RpcPlayVoice(byte[] compressedVoice)
    {
        uint optimalRate = SteamUser.GetVoiceOptimalSampleRate();
        // Створюємо буфер для розпакованого звуку (Steam використовує 16-bit PCM)
        byte[] uncompressedDestBuffer = new byte[22050 * 2];
        uint bytesWritten;

        // Розпаковуємо дані
        EVoiceResult res = SteamUser.DecompressVoice(
            compressedVoice, (uint)compressedVoice.Length,
            uncompressedDestBuffer, (uint)uncompressedDestBuffer.Length,
            out bytesWritten, optimalRate);

        if (res == EVoiceResult.k_EVoiceResultOK && bytesWritten > 0)
        {
            // Конвертуємо байти (16-bit PCM) у float (від -1.0 до 1.0) для Unity
            float[] floatArray = new float[bytesWritten / 2];
            for (int i = 0; i < floatArray.Length; i++)
            {
                short val = BitConverter.ToInt16(uncompressedDestBuffer, i * 2);
                floatArray[i] = val / 32768f;
            }

            // Додаємо звук у чергу на відтворення (використовуємо lock для безпеки потоків)
            lock (audioBuffer)
            {
                foreach (float f in floatArray)
                {
                    audioBuffer.Enqueue(f);
                }
            }
        }
    }

    // Вбудований метод Unity: викликається аудіодвижком, коли йому потрібні дані для відтворення
    void OnAudioFilterRead(float[] data, int channels)
    {
        // Не програємо звук самі собі
        if (isLocalPlayer) return;

        lock (audioBuffer)
        {
            for (int i = 0; i < data.Length; i += channels)
            {
                // Беремо звук з черги. Якщо я мовчу - черга пуста, повертаємо 0 (тишу)
                float sample = audioBuffer.Count > 0 ? audioBuffer.Dequeue() : 0f;

                // Копіюємо звук на всі канали (лівий/правий динаміки)
                for (int j = 0; j < channels; j++)
                {
                    data[i + j] = sample;
                }
            }
        }
    }
}