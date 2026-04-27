using Network;
using UnityEngine;
using UnityEngine.UI;

public class RobotModePanel : MonoBehaviour
{
    public Text robotModeText;

    private void OnEnable()
    {
        UdpReceiver.ReceiveEvent += OnUdpReceive;
    }

    private void OnDisable()
    {
        UdpReceiver.ReceiveEvent -= OnUdpReceive;
    }

    private void OnUdpReceive(NetPacket packet)
    {
        // Assuming the command for robot mode is 0x1001
        // and the data is a string.
        // You might need to adjust the command and data parsing
        // based on your actual data protocol.
        if (packet.Cmd == 0x1001)
        {
            string robotMode = packet.ToString();
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (robotModeText != null)
                {
                    robotModeText.text = robotMode;
                }
            });
        }
    }
}
