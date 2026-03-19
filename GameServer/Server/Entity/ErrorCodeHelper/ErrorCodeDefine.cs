namespace Fantasy;

public static class ErrorCodeDefine
{
    public const uint SUCCESS = 0; // 响应处理成功
    public const uint REGISTER_ACCOUNT_EXISTS = 1001; // 账号已存在
    public const uint REGISTER_UNKNOW_EORROR = 1002; // 未知错误
    public const uint REGISTER_INVALID_PARAMETER = 1003; // 参数无效，账号或密码为空
    public const uint LOGIN_INCORRECT_PASSWORD = 1004; // 密码错误，请重试
    public const uint LOGIN_ACCOUNT_NOT_EXIST = 1005; // 账号不存在
    public const uint LOGIN_ACCOUNT_EXISTS_PASSWORD_ERROR = 1006; // 账号已存在但密码错误
    public const uint LOGIN_UNKNOW_EORROR = 1007; // 未知错误
    public const uint LOGIN_INVALID_PARAMETER = 1008; // 参数无效，账号或密码为空
    public const uint LOGIN_ACCOUNT_ALREADY_ONLINE = 1009; // 账号已在线
    public const uint LOGIN_REGISTER_ACCOUNT_OR_PASSWORD_NOT_EMPTY = 1010; // 账号或密码不能为空
    public const uint LOGIN_REGISTER_INCORRECT_FORMAT = 1011; // 账号格式不正确，需6-24位字母和数字
    public const uint LOGIN_REGISTER_PASSWORD_LESS_LIMIT = 1012; // 密码长度至少需8位
    public const uint REGISTER_TOW_PASSWORD_INCONSISTENT = 1013; // 两次输入的密码不一致
    public const uint REGISTER_SUCCESS = 1014; // 注册成功
    
    public const uint CREATE_ROLE_SUCCESS = 2001; // 创建角色成功
    public const uint ROLE_CONFIG_NOT_FOUND = 2002; // 创角色配置不存在
    public const uint ROLE_NAME_DUPLICATE = 2003; // 角色名已存在
    public const uint    ROLE_NAME_INVALID  = 2004; // 角色名无效（为空或包含非法字符）
    public const uint ROLE_SEX_INVALID   = 2005; // 性别参数无效
    public const uint     ROLE_NOT_FOUND    = 2006; // 角色不存在
    public const uint ROLE_NOT_BELONG_TO_ACCOUNT  = 2007; // 角色不属于该账号
    public const uint     ROLE_NAME_LENGTH_OUT_RANGE  = 2008; // 角色名长度不符，请使用中文2-6字，英文4-12字符
    public const uint ROLE_NAME_CHAR_INVALID   = 2009; // 角色名字含有非法字符
}