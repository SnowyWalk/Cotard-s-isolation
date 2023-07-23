
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System;

namespace yoshio_will.RespawnDisabler
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class RespawnDisabler : UdonSharpBehaviour
    {
        [SerializeField] private Transform SpawnPoint;
        [SerializeField] private bool PreventRespawn = true;
        [SerializeField] private bool PreventRejoin = true;
        [SerializeField] private float RejoinResetTimer = 180f;

        [UdonSynced(UdonSyncMode.None)] private Vector3[] _playersPos;
        [UdonSynced(UdonSyncMode.None)] private Quaternion[] _playersRot;
        [UdonSynced(UdonSyncMode.None)] private string[] _playersDisplayName;
        [UdonSynced(UdonSyncMode.None)] private long[] _leaveTime;

        private long _rejoinResetTimerTicks;
        private VRCPlayerApi _localPlayer;

        void Start()
        {
            // 空の配列を用意
            _playersPos = new Vector3[0];
            _playersRot = new Quaternion[0];
            _playersDisplayName = new string[0];
            _leaveTime = new long[0];

            _rejoinResetTimerTicks = (long)(RejoinResetTimer * 10000000f);
            _localPlayer = Networking.LocalPlayer;
        }

        private void Update()
        {
            // リスポーン阻止処理
            if (PreventRespawn)
            {
                SpawnPoint.position = _localPlayer.GetPosition();
                SpawnPoint.rotation = _localPlayer.GetRotation();
            }

            if (!Networking.IsOwner(gameObject)) return;

            // 掃除が必要か？
            int expiredCount = 0, idx;
            long expireTicks = DateTime.Now.Ticks - _rejoinResetTimerTicks;
            for (idx = 0; idx < _playersDisplayName.Length; idx ++)
            {
                if (_leaveTime[idx] < expireTicks) expiredCount++;
            }
            if (expiredCount == 0) return;

            // 掃除する
            // 新しい配列を用意する
            int newArraySize = _playersDisplayName.Length - expiredCount;
            Vector3[] _newPlayersPos = new Vector3[newArraySize];
            Quaternion[] _newPlayersRot = new Quaternion[newArraySize];
            string[] _newPlayersDisplayName = new string[newArraySize];
            long[] _newLeaveTime = new long[newArraySize];

            // 現行の配列を新配列にコピー
            int destIdx = 0;
            for (idx = 0; idx < _playersDisplayName.Length; idx++)
            {
                if (_leaveTime[idx] >= expireTicks)
                {
                    // 期限切れじゃないやつだけコピー
                    _newPlayersPos[destIdx] = _playersPos[idx];
                    _newPlayersRot[destIdx] = _playersRot[idx];
                    _newPlayersDisplayName[destIdx] = _playersDisplayName[idx];
                    _newLeaveTime[destIdx] = _leaveTime[idx];
                    destIdx++;
                }
            }

            // 新配列を元のやつにする
            _playersPos = _newPlayersPos;
            _playersRot = _newPlayersRot;
            _playersDisplayName = _newPlayersDisplayName;
            _leaveTime = _newLeaveTime;

            RequestSerialization();
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) return;
            if (!Networking.IsOwner(gameObject)) return;

            int idx;
            string name = player.displayName;
            Vector3 pos = player.GetPosition();
            Quaternion rot = player.GetRotation();

            // リストに既にいないか調べる
            foreach(var listName in _playersDisplayName)
            {
                if (listName == name) return;
            }

            // 新しい配列を用意する
            int newArraySize = _playersDisplayName.Length + 1;
            Vector3[] _newPlayersPos = new Vector3[newArraySize];
            Quaternion[] _newPlayersRot = new Quaternion[newArraySize];
            string[] _newPlayersDisplayName = new string[newArraySize];
            long[] _newLeaveTime = new long[newArraySize];

            // 現行の配列を新配列にコピー
            for (idx = 0; idx < _playersDisplayName.Length; idx ++)
            {
                _newPlayersPos[idx] = _playersPos[idx];
                _newPlayersRot[idx] = _playersRot[idx];
                _newPlayersDisplayName[idx] = _playersDisplayName[idx];
                _newLeaveTime[idx] = _leaveTime[idx];
            }

            // 新配列の最後に新しいプレイヤーを入れる
            idx = _newPlayersDisplayName.Length - 1;
            _newPlayersPos[idx] = pos;
            _newPlayersRot[idx] = rot;
            _newPlayersDisplayName[idx] = name;
            _newLeaveTime[idx] = DateTime.Now.Ticks;

            // 新配列を元のやつにする
            _playersPos = _newPlayersPos;
            _playersRot = _newPlayersRot;
            _playersDisplayName = _newPlayersDisplayName;
            _leaveTime = _newLeaveTime;

            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            // 同期されてきた配列に自分が含まれてないか調べる
            bool isImInList = false;
            string myName = _localPlayer.displayName;
            int idx;
            for (idx = 0; idx < _playersDisplayName.Length; idx++)
            {
                if (_playersDisplayName[idx] == myName)
                {
                    isImInList = true;
                    break;
                }
            }

            if (!isImInList) return;

            // 配列に入っている座標にテレポートする
            if (PreventRejoin) _localPlayer.TeleportTo(_playersPos[idx], _playersRot[idx]);

            // 配列から自分を取り除く
            Networking.SetOwner(_localPlayer, gameObject);
            // 新しい配列を用意する
            int newArraySize = _playersDisplayName.Length - 1;
            Vector3[] _newPlayersPos = new Vector3[newArraySize];
            Quaternion[] _newPlayersRot = new Quaternion[newArraySize];
            string[] _newPlayersDisplayName = new string[newArraySize];
            long[] _newLeaveTime = new long[newArraySize];

            // 現行の配列を新配列にコピー
            int destIdx = 0;
            for (idx = 0; idx < _playersDisplayName.Length; idx++)
            {
                if (_playersDisplayName[idx] != myName)
                {
                    // 自分以外だけコピー
                    _newPlayersPos[destIdx] = _playersPos[idx];
                    _newPlayersRot[destIdx] = _playersRot[idx];
                    _newPlayersDisplayName[destIdx] = _playersDisplayName[idx];
                    _newLeaveTime[destIdx] = _leaveTime[idx];
                    destIdx++;
                }
            }

            // 新配列を元のやつにする
            _playersPos = _newPlayersPos;
            _playersRot = _newPlayersRot;
            _playersDisplayName = _newPlayersDisplayName;
            _leaveTime = _newLeaveTime;

            RequestSerialization();
        }

    }
}