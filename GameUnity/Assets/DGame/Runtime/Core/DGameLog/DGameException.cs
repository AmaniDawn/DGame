using System;
using System.Runtime.Serialization;

namespace DGame
{
    /// <summary>
    /// 异常类
    /// </summary>
    [Serializable]
    public class DGameException : Exception
    {
        /// <summary>
        /// 初始化新实例
        /// </summary>
        public DGameException() : base()
        {

        }

        /// <summary>
        /// 使用指定错误消息初始化异常类新实例
        /// </summary>
        /// <param name="msg">描述错误的消息</param>
        public DGameException(string msg) : base(msg)
        {

        }

        /// <summary>
        /// 使用指定错误消息和对作为异常原因的内部异常的引用来初始化异常类的新实例
        /// </summary>
        /// <param name="msg">解释异常原因的错误消息</param>
        /// <param name="innerException">导致当前异常的异常。如果 innerException 参数不为空引用，则在处理内部异常的 catch 块中引发当前异常。</param>
        public DGameException(string msg, Exception innerException) : base(msg, innerException)
        {

        }

        /// <summary>
        /// 用与劣化数据初始化异常类新实例
        /// </summary>
        /// <param name="info">存有有关引发异常的序列化对象数据</param>
        /// <param name="context">包含有关源或目标的上下文信息</param>
        public DGameException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }

        /// <summary>
        /// 重写ToString方法，在输出前添加[DGame]前缀
        /// </summary>
        /// <returns>带有[DGame]前缀的异常信息</returns>
        public override string ToString() => $"<color=#B23333><b>[{DateTime.Now:HH:mm:ss-fff}] [DGameEXCEPTION] ► </b></color> - <color=red>{base.ToString()}</color>";

        /// <summary>
        /// 重写Message属性，在消息前添加[DGame]前缀（可选）
        /// </summary>
        public override string Message =>  $"<color=#B23333><b>[{DateTime.Now:HH:mm:ss-fff}] [DGameEXCEPTION] ► </b></color> - <color=red>{base.Message}</color>";
    }
}