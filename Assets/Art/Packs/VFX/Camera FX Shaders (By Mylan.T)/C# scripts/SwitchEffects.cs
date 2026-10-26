using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Threading.Tasks;
using TMPro;
namespace SwitchEffects
{
    public class SwitchEffects : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created

        public TextMeshProUGUI fpsText;

        private int frameCount = 0;
        private float elapsedTime = 0f;
        private float refreshRate = 1f;

        void Update()
        {
            frameCount++;
            elapsedTime += Time.unscaledDeltaTime;

            if (elapsedTime >= refreshRate)
            {
                int fps = Mathf.RoundToInt(frameCount / elapsedTime);
                fpsText.text = $"FPS: {fps}";

                // Reset counters
                frameCount = 0;
                elapsedTime = 0f;
            }
        }
        void Start()
        {
            blurStrengthSlider.value = blurEf.blurStrength;

            glitchIntensitySlider.value = glitchEf.intensity;
            glitchTimeSpeedSlider.value = glitchEf.timeSpeed;
            glitchBlockSize.value = glitchEf.blockSize;

            retroScanlineIntensitySlider.value = retroEf.scanlineIntensity;
            retroColorFadeSlider.value = retroEf.colorFade;
            retroNoiseStrengthSlider.value = retroEf.noiseStrength;
            retroDistorionSlider.value = retroEf.distortion;

            dotMaskDotSizeSlider.value = dotMaskEf.dotSize;
            dotMaskDotIntensitySlider.value = dotMaskEf.dotIntensity;

            BlinkingDotSizeSlider.value = blinkingDotEf.dotSize;
            BlinkingDotIntensitySlider.value = blinkingDotEf.dotIntensity;
            BlinkingDotBlinkSpeedSlider.value = blinkingDotEf.blinkSpeed;
            BlinkingDotBlinkStrengthSlider.value = blinkingDotEf.blinkStrength;

            bulgedDistorionAmountSlider.value = bulgedEf.distortionAmount;

            NauseaWaveStrengthSlider.value = nauseaEf.waveStrength;
            NauseaWaveSpeedSlider.value = nauseaEf.waveSpeed;
            NauseaChromaticAmountSlider.value = nauseaEf.chromaticAmount;

            ColorPulsePulseSpeedSlider.value = colorPulseEf.pulseSpeed;
            ColorPulseIntensitySlider.value = colorPulseEf.intensity;



            blurStrengthSlider.onValueChanged.AddListener(BlurStrengthVoid);

            glitchIntensitySlider.onValueChanged.AddListener(GlitchIntensityVoid);
            glitchTimeSpeedSlider.onValueChanged.AddListener(GlitchTimeSpeedVoid);
            glitchBlockSize.onValueChanged.AddListener(GlitchBlickSizeVoid);

            retroScanlineIntensitySlider.onValueChanged.AddListener(RetroScanlineIntensityVoid);
            retroColorFadeSlider.onValueChanged.AddListener(RetroColorFadeVoid);
            retroNoiseStrengthSlider.onValueChanged.AddListener(RetroNoiseStrengthVoid);
            retroDistorionSlider.onValueChanged.AddListener(RetroDistorionVoid);

            dotMaskDotSizeSlider.onValueChanged.AddListener(DotMaskDotSizeVoid);
            dotMaskDotIntensitySlider.onValueChanged.AddListener(DotMaskDotIntensityVoid);

            BlinkingDotSizeSlider.onValueChanged.AddListener(BlinkingDotDotSize);
            BlinkingDotIntensitySlider.onValueChanged.AddListener(BlinkingDotDotIntensityVoid);
            BlinkingDotBlinkSpeedSlider.onValueChanged.AddListener(BlinkingDotBlinkSpeedVoid);
            BlinkingDotBlinkStrengthSlider.onValueChanged.AddListener(BlinkingDotBlinkStrengthVoid);

            bulgedDistorionAmountSlider.onValueChanged.AddListener(BulgedDistortionAmountVoid);

            NauseaWaveStrengthSlider.onValueChanged.AddListener(NauseaWaveStrengthVoid);
            NauseaWaveSpeedSlider.onValueChanged.AddListener(NauseaWaveSpeedVoid);
            NauseaChromaticAmountSlider.onValueChanged.AddListener(NauseaChromaticAmountVoid);

            ColorPulsePulseSpeedSlider.onValueChanged.AddListener(ColorPulsePulseSpeedVoid);
            ColorPulseIntensitySlider.onValueChanged.AddListener(ColorPulseIntensityVoid);

            currentEFfect = 1;
            CheckVoid();

            nextEffectButton.onClick.AddListener(NextVoid);
            prevEffectButton.onClick.AddListener(PrevVoid);


        }

        void BlurStrengthVoid(float v)
        {
            blurEf.blurStrength = v;
        }
        void GlitchIntensityVoid(float v)
        {
            glitchEf.intensity = v;
        }
        void GlitchTimeSpeedVoid(float v)
        {
            glitchEf.timeSpeed = v;
        }
        void GlitchBlickSizeVoid(float v)
        {
            glitchEf.blockSize = v;
        }
        void RetroScanlineIntensityVoid(float v)
        {
            retroEf.scanlineIntensity = v;
        }
        void RetroColorFadeVoid(float v)
        {
            retroEf.colorFade = v;
        }
        void RetroNoiseStrengthVoid(float v)
        {
            retroEf.noiseStrength = v;
        }
        void RetroDistorionVoid(float v)
        {
            retroEf.distortion = v;
        }
        void DotMaskDotSizeVoid(float v)
        {
            dotMaskEf.dotSize = v;
        }
        void DotMaskDotIntensityVoid(float v)
        {
            dotMaskEf.dotIntensity = v;
        }
        void BlinkingDotDotSize(float v)
        {
            blinkingDotEf.dotSize = v;
        }
        void BlinkingDotDotIntensityVoid(float v)
        {
            blinkingDotEf.dotIntensity = v;
        }
        void BlinkingDotBlinkSpeedVoid(float v)
        {
            blinkingDotEf.blinkSpeed = v;
        }
        void BlinkingDotBlinkStrengthVoid(float v)
        {
            blinkingDotEf.blinkStrength = v;
        }
        void BulgedDistortionAmountVoid(float v)
        {
            bulgedEf.distortionAmount = v;
        }
        void NauseaWaveStrengthVoid(float v)
        {
            nauseaEf.waveStrength = v;
        }
        void NauseaWaveSpeedVoid(float v)
        {
            nauseaEf.waveSpeed = v;
        }
        void NauseaChromaticAmountVoid(float v)
        {
            nauseaEf.chromaticAmount = v;
        }
        void ColorPulsePulseSpeedVoid(float v)
        {
            colorPulseEf.pulseSpeed = v;
        }
        void ColorPulseIntensityVoid(float v)
        {
            colorPulseEf.intensity = v;
        }
        void NextVoid()
        {
            currentEFfect++;
            CheckVoid();
        }
        void PrevVoid()
        {
            currentEFfect--;
            CheckVoid();
        }

        void CheckVoid()
        {
            if (currentEFfect == 1)
            {
                currentEffectsTextsObjs[1].SetActive(true);
                currentEffectsTextsObjs[2].SetActive(false);
                currentEffectsTextsObjs[3].SetActive(false);
                currentEffectsTextsObjs[4].SetActive(false);
                currentEffectsTextsObjs[5].SetActive(false);
                currentEffectsTextsObjs[6].SetActive(false);
                currentEffectsTextsObjs[7].SetActive(false);
                currentEffectsTextsObjs[8].SetActive(false);

                prevEffectButton.gameObject.SetActive(false);
                nextEffectButton.gameObject.SetActive(true);

                blurEf.enabled = true;
                glitchEf.enabled = false;
                retroEf.enabled = false;
                dotMaskEf.enabled = false;
                blinkingDotEf.enabled = false;
                bulgedEf.enabled = false;
                nauseaEf.enabled = false;
                colorPulseEf.enabled = false;

                slidersObjs[1].SetActive(true);
                slidersObjs[2].SetActive(false);
                slidersObjs[3].SetActive(false);
                slidersObjs[4].SetActive(false);
                slidersObjs[5].SetActive(false);
                slidersObjs[6].SetActive(false);
                slidersObjs[7].SetActive(false);
                slidersObjs[8].SetActive(false);
            }
            else if (currentEFfect == 2)
            {
                currentEffectsTextsObjs[1].SetActive(false);
                currentEffectsTextsObjs[2].SetActive(true);
                currentEffectsTextsObjs[3].SetActive(false);
                currentEffectsTextsObjs[4].SetActive(false);
                currentEffectsTextsObjs[5].SetActive(false);
                currentEffectsTextsObjs[6].SetActive(false);
                currentEffectsTextsObjs[7].SetActive(false);
                currentEffectsTextsObjs[8].SetActive(false);

                prevEffectButton.gameObject.SetActive(true);
                nextEffectButton.gameObject.SetActive(true);

                blurEf.enabled = false;
                glitchEf.enabled = true;
                retroEf.enabled = false;
                dotMaskEf.enabled = false;
                blinkingDotEf.enabled = false;
                bulgedEf.enabled = false;
                nauseaEf.enabled = false;
                colorPulseEf.enabled = false;

                slidersObjs[1].SetActive(false);
                slidersObjs[2].SetActive(true);
                slidersObjs[3].SetActive(false);
                slidersObjs[4].SetActive(false);
                slidersObjs[5].SetActive(false);
                slidersObjs[6].SetActive(false);
                slidersObjs[7].SetActive(false);
                slidersObjs[8].SetActive(false);
            }
            else if (currentEFfect == 3)
            {
                currentEffectsTextsObjs[1].SetActive(false);
                currentEffectsTextsObjs[2].SetActive(false);
                currentEffectsTextsObjs[3].SetActive(true);
                currentEffectsTextsObjs[4].SetActive(false);
                currentEffectsTextsObjs[5].SetActive(false);
                currentEffectsTextsObjs[6].SetActive(false);
                currentEffectsTextsObjs[7].SetActive(false);
                currentEffectsTextsObjs[8].SetActive(false);

                prevEffectButton.gameObject.SetActive(true);
                nextEffectButton.gameObject.SetActive(true);

                blurEf.enabled = false;
                glitchEf.enabled = false;
                retroEf.enabled = true;
                dotMaskEf.enabled = false;
                blinkingDotEf.enabled = false;
                bulgedEf.enabled = false;
                nauseaEf.enabled = false;
                colorPulseEf.enabled = false;

                slidersObjs[1].SetActive(false);
                slidersObjs[2].SetActive(false);
                slidersObjs[3].SetActive(true);
                slidersObjs[4].SetActive(false);
                slidersObjs[5].SetActive(false);
                slidersObjs[6].SetActive(false);
                slidersObjs[7].SetActive(false);
                slidersObjs[8].SetActive(false);
            }
            else if (currentEFfect == 4)
            {
                currentEffectsTextsObjs[1].SetActive(false);
                currentEffectsTextsObjs[2].SetActive(false);
                currentEffectsTextsObjs[3].SetActive(false);
                currentEffectsTextsObjs[4].SetActive(true);
                currentEffectsTextsObjs[5].SetActive(false);
                currentEffectsTextsObjs[6].SetActive(false);
                currentEffectsTextsObjs[7].SetActive(false);
                currentEffectsTextsObjs[8].SetActive(false);

                prevEffectButton.gameObject.SetActive(true);
                nextEffectButton.gameObject.SetActive(true);

                blurEf.enabled = false;
                glitchEf.enabled = false;
                retroEf.enabled = false;
                dotMaskEf.enabled = true;
                blinkingDotEf.enabled = false;
                bulgedEf.enabled = false;
                nauseaEf.enabled = false;
                colorPulseEf.enabled = false;

                slidersObjs[1].SetActive(false);
                slidersObjs[2].SetActive(false);
                slidersObjs[3].SetActive(false);
                slidersObjs[4].SetActive(true);
                slidersObjs[5].SetActive(false);
                slidersObjs[6].SetActive(false);
                slidersObjs[7].SetActive(false);
                slidersObjs[8].SetActive(false);
            }
            else if (currentEFfect == 5)
            {
                currentEffectsTextsObjs[1].SetActive(false);
                currentEffectsTextsObjs[2].SetActive(false);
                currentEffectsTextsObjs[3].SetActive(false);
                currentEffectsTextsObjs[4].SetActive(false);
                currentEffectsTextsObjs[5].SetActive(true);
                currentEffectsTextsObjs[6].SetActive(false);
                currentEffectsTextsObjs[7].SetActive(false);
                currentEffectsTextsObjs[8].SetActive(false);

                prevEffectButton.gameObject.SetActive(true);
                nextEffectButton.gameObject.SetActive(true);

                blurEf.enabled = false;
                glitchEf.enabled = false;
                retroEf.enabled = false;
                dotMaskEf.enabled = false;
                blinkingDotEf.enabled = true;
                bulgedEf.enabled = false;
                nauseaEf.enabled = false;
                colorPulseEf.enabled = false;

                slidersObjs[1].SetActive(false);
                slidersObjs[2].SetActive(false);
                slidersObjs[3].SetActive(false);
                slidersObjs[4].SetActive(false);
                slidersObjs[5].SetActive(true);
                slidersObjs[6].SetActive(false);
                slidersObjs[7].SetActive(false);
                slidersObjs[8].SetActive(false);
            }
            else if (currentEFfect == 6)
            {
                currentEffectsTextsObjs[1].SetActive(false);
                currentEffectsTextsObjs[2].SetActive(false);
                currentEffectsTextsObjs[3].SetActive(false);
                currentEffectsTextsObjs[4].SetActive(false);
                currentEffectsTextsObjs[5].SetActive(false);
                currentEffectsTextsObjs[6].SetActive(true);
                currentEffectsTextsObjs[7].SetActive(false);
                currentEffectsTextsObjs[8].SetActive(false);

                prevEffectButton.gameObject.SetActive(true);
                nextEffectButton.gameObject.SetActive(true);

                blurEf.enabled = false;
                glitchEf.enabled = false;
                retroEf.enabled = false;
                dotMaskEf.enabled = false;
                blinkingDotEf.enabled = false;
                bulgedEf.enabled = true;
                nauseaEf.enabled = false;
                colorPulseEf.enabled = false;

                slidersObjs[1].SetActive(false);
                slidersObjs[2].SetActive(false);
                slidersObjs[3].SetActive(false);
                slidersObjs[4].SetActive(false);
                slidersObjs[5].SetActive(false);
                slidersObjs[6].SetActive(true);
                slidersObjs[7].SetActive(false);
                slidersObjs[8].SetActive(false);
            }
            else if (currentEFfect == 7)
            {
                currentEffectsTextsObjs[1].SetActive(false);
                currentEffectsTextsObjs[2].SetActive(false);
                currentEffectsTextsObjs[3].SetActive(false);
                currentEffectsTextsObjs[4].SetActive(false);
                currentEffectsTextsObjs[5].SetActive(false);
                currentEffectsTextsObjs[6].SetActive(false);
                currentEffectsTextsObjs[7].SetActive(true);
                currentEffectsTextsObjs[8].SetActive(false);

                prevEffectButton.gameObject.SetActive(true);
                nextEffectButton.gameObject.SetActive(true);

                blurEf.enabled = false;
                glitchEf.enabled = false;
                retroEf.enabled = false;
                dotMaskEf.enabled = false;
                blinkingDotEf.enabled = false;
                bulgedEf.enabled = false;
                nauseaEf.enabled = true;
                colorPulseEf.enabled = false;

                slidersObjs[1].SetActive(false);
                slidersObjs[2].SetActive(false);
                slidersObjs[3].SetActive(false);
                slidersObjs[4].SetActive(false);
                slidersObjs[5].SetActive(false);
                slidersObjs[6].SetActive(false);
                slidersObjs[7].SetActive(true);
                slidersObjs[8].SetActive(false);
            }
            else if (currentEFfect == 8)
            {
                currentEffectsTextsObjs[1].SetActive(false);
                currentEffectsTextsObjs[2].SetActive(false);
                currentEffectsTextsObjs[3].SetActive(false);
                currentEffectsTextsObjs[4].SetActive(false);
                currentEffectsTextsObjs[5].SetActive(false);
                currentEffectsTextsObjs[6].SetActive(false);
                currentEffectsTextsObjs[7].SetActive(false);
                currentEffectsTextsObjs[8].SetActive(true);

                prevEffectButton.gameObject.SetActive(true);
                nextEffectButton.gameObject.SetActive(false);

                blurEf.enabled = false;
                glitchEf.enabled = false;
                retroEf.enabled = false;
                dotMaskEf.enabled = false;
                blinkingDotEf.enabled = false;
                bulgedEf.enabled = false;
                nauseaEf.enabled = false;
                colorPulseEf.enabled = true;

                slidersObjs[1].SetActive(false);
                slidersObjs[2].SetActive(false);
                slidersObjs[3].SetActive(false);
                slidersObjs[4].SetActive(false);
                slidersObjs[5].SetActive(false);
                slidersObjs[6].SetActive(false);
                slidersObjs[7].SetActive(false);
                slidersObjs[8].SetActive(true);
            }
        }

        public List<GameObject> currentEffectsTextsObjs;
        public List<GameObject> slidersObjs;
        public Button nextEffectButton;
        public Button prevEffectButton;
        private int currentEFfect;

        [Space(20)]
        [Header("effects references")]
        public glitch.GlitchEffect glitchEf;
        public blur.CameraBlur blurEf;
        public retro.RetroEffect retroEf;
        public dotMask.DotMaskEffect dotMaskEf;
        public blinkingDot.BlinkingDotEffect blinkingDotEf;
        public bulged.BulgedEffect bulgedEf;
        public Nausea.NauseaEffect nauseaEf;
        public colorPulse.ColorPulseEffect colorPulseEf;

        [Space(20)]
        [Header("Blur Sliders")]
        public Slider blurStrengthSlider;
        [Space(20)]
        [Header("Glitch Sliders")]
        public Slider glitchIntensitySlider;
        public Slider glitchTimeSpeedSlider;
        public Slider glitchBlockSize;
        [Space(20)]
        [Header("Retro Sliders")]
        public Slider retroScanlineIntensitySlider;
        public Slider retroColorFadeSlider;
        public Slider retroNoiseStrengthSlider;
        public Slider retroDistorionSlider;
        [Space(20)]
        [Header("Dot mask Sliders")]
        public Slider dotMaskDotSizeSlider;
        public Slider dotMaskDotIntensitySlider;
        [Space(20)]
        [Header("Blinking dot Sliders")]
        public Slider BlinkingDotSizeSlider;
        public Slider BlinkingDotIntensitySlider;
        public Slider BlinkingDotBlinkSpeedSlider;
        public Slider BlinkingDotBlinkStrengthSlider;
        [Space(20)]
        [Header("Bulged Sliders")]
        public Slider bulgedDistorionAmountSlider;
        [Space(20)]
        [Header("Nausea Sliders")]
        public Slider NauseaWaveStrengthSlider;
        public Slider NauseaWaveSpeedSlider;
        public Slider NauseaChromaticAmountSlider;
        [Space(20)]
        [Header("Color pulse Sliders")]
        public Slider ColorPulsePulseSpeedSlider;
        public Slider ColorPulseIntensitySlider;


    }

}
