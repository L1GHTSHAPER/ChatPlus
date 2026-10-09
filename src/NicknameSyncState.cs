namespace ChatPlus
{
    // Wait for the latest style to settle and rate-limit retries while the native RPC acknowledgement arrives.
    internal sealed class NicknameSyncState
    {
        const float SettleTime = .35f;
        const float RetryTime = 2f;
        string _desired;
        float _settleAt, _retryAt;

        internal bool ShouldSend(string desired, string current, float now)
        {
            if (desired != _desired)
            {
                _desired = desired;
                _settleAt = now + SettleTime;
            }
            if (desired == current || now < _settleAt || now < _retryAt) return false;
            _retryAt = now + RetryTime;
            return true;
        }

        internal void Reset()
        {
            _desired = null;
            _settleAt = _retryAt = 0f;
        }
    }
}
