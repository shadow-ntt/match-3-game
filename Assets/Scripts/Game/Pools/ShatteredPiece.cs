using System;
using DG.Tweening;
using UnityEngine;

// Dieu khien 1 manh vo block: bay ra, xoay, mo dan va thu hoi ve pool
[RequireComponent(typeof(SpriteRenderer))]
public class ShatteredPiece : MonoBehaviour
{
    private SpriteRenderer _sr;
    private Action<ShatteredPiece> _onFinished;

    private Tween _moveTween;
    private Tween _rotateTween;
    private Tween _scaleTween;
    private Tween _fadeTween;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    public void SetSorting(int order, string sortingLayerName = "Default")
    {
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        if (_sr != null)
        {
            _sr.sortingLayerName = sortingLayerName;
            _sr.sortingOrder = order;
        }
    }

    public void Play(
        Vector3 worldPos,
        Sprite sprite,
        Vector2 direction,
        float speed,
        float duration,
        float startScale,
        Action<ShatteredPiece> onFinished)
    {
        KillTweens();

        _onFinished = onFinished;
        transform.position = worldPos;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one * (startScale > 0f ? startScale : 1f);

        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        _sr.sprite = sprite;
        _sr.color = Color.white;

        gameObject.SetActive(true);

        Vector3 targetPos = worldPos + (Vector3)(direction.normalized * speed);
        float randomRotation = UnityEngine.Random.Range(-360f, 360f);

        _moveTween = transform.DOMove(targetPos, duration).SetEase(Ease.OutQuad);
        _rotateTween = transform.DORotate(new Vector3(0, 0, randomRotation), duration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad);
        _scaleTween = transform.DOScale(Vector3.zero, duration).SetEase(Ease.InQuad);
        _fadeTween = _sr.DOFade(0f, duration * 0.45f).SetDelay(duration * 0.55f).OnComplete(ReturnToPool);
    }

    private void ReturnToPool()
    {
        KillTweens();
        gameObject.SetActive(false);
        _onFinished?.Invoke(this);
    }

    private void KillTweens()
    {
        if (_moveTween != null && _moveTween.IsActive()) _moveTween.Kill();
        if (_rotateTween != null && _rotateTween.IsActive()) _rotateTween.Kill();
        if (_scaleTween != null && _scaleTween.IsActive()) _scaleTween.Kill();
        if (_fadeTween != null && _fadeTween.IsActive()) _fadeTween.Kill();
        _moveTween = null;
        _rotateTween = null;
        _scaleTween = null;
        _fadeTween = null;
    }

    private void OnDisable()
    {
        KillTweens();
    }

    private void OnDestroy()
    {
        KillTweens();
    }
}
