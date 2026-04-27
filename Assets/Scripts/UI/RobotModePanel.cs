using UnityEngine;
using UnityEngine.UI;

public class RobotModePanel : MonoBehaviour
{
    public Text robotModeText;

    private void OnEnable()
    {
        // 订阅新的TCP监听器事件
        RobotModeTcpListener.OnRobotModeReceived += OnReceiveRobotMode;
    }

    private void OnDisable()
    {
        // 取消订阅
        RobotModeTcpListener.OnRobotModeReceived -= OnReceiveRobotMode;
    }

    private void OnReceiveRobotMode(string mode)
    {
        // 使用UnityMainThreadDispatcher确保UI更新在主线程执行
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if (robotModeText != null)
            {
                robotModeText.text = mode;
            }
        });
    }
}