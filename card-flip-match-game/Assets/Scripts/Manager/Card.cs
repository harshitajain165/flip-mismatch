using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CardMatching.GamePlay
{
    public class Card : MonoBehaviour
    {
        public int SpriteId { get; private set; }
        public bool IsMatched { get; private set; }
        public bool IsFaceUp { get; private set; }

        [Header("References")]
        [SerializeField] private Image frontImage;
        [SerializeField] private Image backImage;

        [Header("Flip settings")]
        [SerializeField] private float flipDuration = 0.2f; // seconds

        // events
        public event Action<Card> OnFlipped; // invoked when flip to face-up completes
        public event Action<Card> OnMatched;

        private Coroutine currentFlip;

        public void Initialize(int spriteId, Sprite sprite)
        {
            SpriteId = spriteId;
            frontImage.sprite = sprite;
            ShowBackInstant();
            IsMatched = false;
            IsFaceUp = false;
        }

        public void SetSize(Vector2 size)
        {
            var rt = GetComponent<RectTransform>();
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        }

        public void SetAnchor(Vector2 min, Vector2 max, Vector2 pivot)
        {
            var rt = GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
        }

        public void SetAnchoredPosition(Vector2 pos)
        {
            var rt = GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
        }

        public void OnPointerClick()
        {
            // call this from EventTrigger/Button or add IPointerClickHandler
            if (IsMatched || IsFaceUp) return;

            // start flip
            if (currentFlip != null) StopCoroutine(currentFlip);
            currentFlip = StartCoroutine(FlipToFrontRoutine());
        }

        private IEnumerator FlipToFrontRoutine()
        {
            yield return FlipRoutine(true);
            currentFlip = null;
            OnFlipped?.Invoke(this);
        }

        public void RevealInstant()
        {
            ShowFrontInstant();
            IsFaceUp = true;
        }

        public IEnumerator FlipToBackRoutine(float waitBefore = 0f)
        {
            if (waitBefore > 0f) yield return new WaitForSeconds(waitBefore);
            if (currentFlip != null) StopCoroutine(currentFlip);
            currentFlip = StartCoroutine(FlipRoutine(false));
            while (currentFlip != null)
                yield return null;
        }

        private IEnumerator FlipRoutine(bool toFront)
        {
            float elapsed = 0f;
            float half = flipDuration / 2f;

            // shrink X to 0
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                float scaleX = Mathf.Lerp(1f, 0f, t);
                transform.localScale = new Vector3(scaleX, 1f, 1f);
                yield return null;
            }

            // swap visuals
            if (toFront)
            {
                ShowFrontInstant();
                IsFaceUp = true;
            }
            else
            {
                ShowBackInstant();
                IsFaceUp = false;
            }

            // expand X to 1
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                float scaleX = Mathf.Lerp(0f, 1f, t);
                transform.localScale = new Vector3(scaleX, 1f, 1f);
                yield return null;
            }

            currentFlip = null;
        }

        public void MarkMatched()
        {
            IsMatched = true;
            OnMatched?.Invoke(this);
        }

        private void ShowFrontInstant()
        {
            frontImage.gameObject.SetActive(true);
            backImage.gameObject.SetActive(false);
        }

        public void ShowBackInstant()
        {
            frontImage.gameObject.SetActive(false);
            backImage.gameObject.SetActive(true);
        }
    }
}
