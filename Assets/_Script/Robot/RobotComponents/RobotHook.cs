using System;
using DG.Tweening;
using Shun_Grid_System;
using UnityEngine;

namespace _Script.Robot
{
    public class RobotHook : MonoBehaviour
    {
        [SerializeField] private Transform _cableTransform;
        [SerializeField] private Transform _hookTransform;    
        [SerializeField] private Transform _topHookCeilingTransform;
        [SerializeField] private Transform _binHookPlaceTransform;
        
        
        [Header("Robot Hook and Cable")]     
        [SerializeField] private float _hookMoveSpeed = 2f;
        [SerializeField] private Ease _hookMoveEase = Ease.InOutCubic;
        [SerializeField] private float _cableLengthMultiply = 3.8f; 
        private Vector3 _cableInitialScale;
        
        
        
        public void ExtendCable(Vector3 worldEndPosition,Action finishCallback)
        {
            Sequence sequence = DOTween.Sequence();
        
            Vector3 topStackPosition = worldEndPosition + (_hookTransform.position - _binHookPlaceTransform.position);
        
            var hookMoveDistance = Mathf.Abs(topStackPosition.y - _topHookCeilingTransform.position.y);
            var hookMoveDuration = hookMoveDistance / _hookMoveSpeed;

            sequence.Join( _hookTransform.DOMove(topStackPosition, hookMoveDuration).SetEase(_hookMoveEase)) ;
        
            sequence.AppendCallback(finishCallback.Invoke);
        }

        public void ContractCable(Action finishCallback)
        {
        
            var hookMoveDistance = Mathf.Abs(_hookTransform.position.y - _topHookCeilingTransform.position.y);
            var hookMoveDuration = hookMoveDistance / _hookMoveSpeed;

            Sequence sequence = DOTween.Sequence();
            sequence.Join( _hookTransform.DOMove(_topHookCeilingTransform.position, hookMoveDuration).SetEase(_hookMoveEase)) ;
        
            sequence.AppendCallback(finishCallback.Invoke);
        }


        public void MoveCable()
        {
            float distance = Vector3.Distance(_topHookCeilingTransform.position, _hookTransform.position);

            _cableTransform.localScale = new Vector3(_cableInitialScale.x, _cableInitialScale.y, distance * _cableLengthMultiply);
            
        }
        
        
    }
}