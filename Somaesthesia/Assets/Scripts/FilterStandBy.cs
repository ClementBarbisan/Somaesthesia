using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class FilterStandBy : MonoBehaviour
{
    private AudioSource _audioSourceStandBy;
    private bool _notStandBy;
    [FormerlySerializedAs("_sprite")] [SerializeField] private SpriteRenderer _spriteOne;
    [FormerlySerializedAs("_sprite")] [SerializeField] private SpriteRenderer _spriteTwo;
    private List<float> _emitNb;
    private List<float> _emitNbBase = new List<float>();

    private bool _filled;

    // Start is called before the first frame update
    void Awake()
    {
        _audioSourceStandBy = GetComponent<AudioSource>();
        _audioSourceStandBy.Play();
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (_notStandBy)
        {
            return;
        }
        int dataLen = data.Length / channels;
        _emitNbBase.Clear();
        _filled = false;
        int n = 0;
        while (n < dataLen)
        {
            
            int i = 0;
            while (i < channels)
            {
                // data[n * channels + i] += x;
                if (data[n * channels + i] > 0)
                {
                    _emitNbBase.Add(data[n * channels + i]);
                }
                i++;
            }
            n++;
        }

        _filled = true;
    }

    private void Update()
    {
        if (!_notStandBy && _audioSourceStandBy.volume < 1f)
        {
            _notStandBy = true;
            _spriteOne.gameObject.SetActive(false);
            _spriteTwo.gameObject.SetActive(false);
            return;
        }
        else if (_notStandBy && _audioSourceStandBy.volume > 0.5f)
        {
            _notStandBy = false;
            _spriteOne.gameObject.SetActive(true);
            _spriteTwo.gameObject.SetActive(true);
        }

        if (_filled && _emitNbBase.Count > 0)
        {
            _emitNb = new List<float>(_emitNbBase);
            _spriteOne.transform.localScale = Vector3.Lerp(_spriteOne.transform.localScale, Vector3.one * ((_emitNb.Max()) * 0.2f), 0.025f);
            _spriteTwo.transform.localScale = Vector3.Lerp(_spriteTwo.transform.localScale, Vector3.one * ((_emitNb.Max()) * 0.2f), 0.025f);
            _emitNb.Clear();
        }
    }
}
