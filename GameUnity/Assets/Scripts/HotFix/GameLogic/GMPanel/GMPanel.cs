using System.Collections.Generic;
using DGame;
using GameProto;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
	public class GMPanel : UIWindow
	{
		#region 脚本工具生成的代码

		private Button m_btnLastExecute;
		private Dropdown m_dropDownSelect;
		private InputField m_inputOldGm;
		private Button m_btnExecuteNoClose;
		private Button m_btnClose;
		private InputField m_inputGm;
		private InputField m_inputGmID;
		private InputField m_inputGmNum;
		private InputField m_inputGmExecute;
		private Button m_btnExecute;
		private Transform m_tfLeftQuickList;
		private Button m_btnServerTemp;
		private Transform m_tfRightQuickList;
		private Button m_btnClientTemp;
		private Text m_textDisplay;
		private Transform m_tfBatchQuickList;
		private Button m_btnBatchTemp;
		private Transform m_tfOtherQuickList;
		private Button m_btnOtherTemp;

		protected override void ScriptGenerator()
		{
			m_btnLastExecute = FindChildComponent<Button>("Content/BottomContent/Operations/m_btnLastExecute");
			m_dropDownSelect = FindChildComponent<Dropdown>("Content/BottomContent/Operations/m_dropDownSelect");
			m_inputOldGm = FindChildComponent<InputField>("Content/BottomContent/Operations/m_inputOldGm");
			m_btnExecuteNoClose = FindChildComponent<Button>("Content/BottomContent/Operations/m_btnExecuteNoClose");
			m_btnClose = FindChildComponent<Button>("Content/BottomContent/Operations/m_btnClose");
			m_inputGm = FindChildComponent<InputField>("Content/BottomContent/Operations/m_inputGm");
			m_inputGmID = FindChildComponent<InputField>("Content/BottomContent/Operations/m_inputGmID");
			m_inputGmNum = FindChildComponent<InputField>("Content/BottomContent/Operations/m_inputGmNum");
			m_inputGmExecute = FindChildComponent<InputField>("Content/BottomContent/Operations/m_inputGmExecute");
			m_btnExecute = FindChildComponent<Button>("Content/BottomContent/Operations/m_btnExecute");
			m_tfLeftQuickList = FindChild("Content/BottomContent/ServerGM/LeftScroll/Viewport/m_tfLeftQuickList");
			m_btnServerTemp = FindChildComponent<Button>("Content/BottomContent/ServerGM/LeftScroll/Viewport/m_tfLeftQuickList/m_btnServerTemp");
			m_tfRightQuickList = FindChild("Content/BottomContent/ClientGM/RightScroll/Viewport/m_tfRightQuickList");
			m_btnClientTemp = FindChildComponent<Button>("Content/BottomContent/ClientGM/RightScroll/Viewport/m_tfRightQuickList/m_btnClientTemp");
			m_textDisplay = FindChildComponent<Text>("Content/TopMsg/DisplayScroll/Viewport/m_textDisplay");
			m_tfBatchQuickList = FindChild("Content/BatchGMBg/BatchScroll/Viewport/m_tfBatchQuickList");
			m_btnBatchTemp = FindChildComponent<Button>("Content/BatchGMBg/BatchScroll/Viewport/m_tfBatchQuickList/m_btnBatchTemp");
			m_tfOtherQuickList = FindChild("Content/OtherGMBg/OtherScroll/Viewport/m_tfOtherQuickList");
			m_btnOtherTemp = FindChildComponent<Button>("Content/OtherGMBg/OtherScroll/Viewport/m_tfOtherQuickList/m_btnOtherTemp");
			m_btnLastExecute.onClick.AddListener(OnClickLastExecuteBtn);
			m_btnExecuteNoClose.onClick.AddListener(OnClickExecuteNoCloseBtn);
			m_btnClose.onClick.AddListener(OnClickCloseBtn);
			m_btnExecute.onClick.AddListener(OnClickExecuteBtn);
			m_btnServerTemp.onClick.AddListener(OnClickServerTempBtn);
			m_btnClientTemp.onClick.AddListener(OnClickClientTempBtn);
			m_btnBatchTemp.onClick.AddListener(OnClickBatchTempBtn);
			m_btnOtherTemp.onClick.AddListener(OnClickOtherTempBtn);
		}

		#endregion

		#region Overrider

		protected override void BindMemberProperty()
		{
			m_dropDownSelect.onValueChanged.AddListener(OnDropdownSelect);
			CreateQuickGmButtons();
		}

		#endregion

		#region 字段

		// private int m_gmCommendIndex = 0;
		private int m_gmCfgCommendIndex = 0;

		private List<GmConfig> m_gmConfigs;

		private List<QuickGmButtonInfo> m_listRmdGmLeft = new List<QuickGmButtonInfo>();
		private List<QuickGmButtonInfo> m_listRmdGmRight = new List<QuickGmButtonInfo>();
		private List<QuickGmButtonInfo> m_listRmdGmOther = new List<QuickGmButtonInfo>();
		private List<QuickGmButtonInfo> m_listRmdGmBatch = new List<QuickGmButtonInfo>();

		private List<QuickGmButtonInfo> ListRmdGmLeft
		{
			get
			{
				if (m_listRmdGmLeft.Count < 1)
				{
					if (m_gmConfigs != null)
					{
						for (int i = 0; i < m_gmConfigs.Count; i++)
						{
							var cfg = m_gmConfigs[i];

							if (cfg.GmType == 2)
							{
								m_listRmdGmLeft.Add(new QuickGmButtonInfo(cfg.GmDesc, cfg.GmOrder, ExecuteCfgGM, cfg));
							}
						}
					}
				}

				return m_listRmdGmLeft;
			}
		}

		private List<QuickGmButtonInfo> ListRmdGmRight
		{
			get
			{
				if (m_listRmdGmRight.Count < 1)
				{
					if (m_gmConfigs != null)
					{
						for (int i = 0; i < m_gmConfigs.Count; i++)
						{
							var cfg = m_gmConfigs[i];

							if (cfg.GmType == 1)
							{
								m_listRmdGmRight.Add(new QuickGmButtonInfo(cfg.GmDesc, cfg.GmOrder, ExecuteCfgGM, cfg));
							}
						}
					}
				}

				return m_listRmdGmRight;
			}
		}

		private List<QuickGmButtonInfo> ListRmdGmOther
		{
			get
			{
				if (m_listRmdGmOther.Count < 1)
				{
					// m_listRmdGmOther.Add(new QuickGmButtonInfo("GC", "gc", OnExecuteGcGm));
				}

				return m_listRmdGmOther;
			}
		}

		#endregion

		#region 函数

		private void CreateQuickGmButtons()
		{
			foreach (var btnInfo in ListRmdGmRight)
			{
				var newButtonGo = GameObject.Instantiate(m_btnClientTemp.gameObject, m_tfRightQuickList);
				newButtonGo.SetActive(true);

				var buttonText = FindChildComponent<Text>(newButtonGo.transform, "Text");
				buttonText.text = btnInfo.GmName;
				var button = newButtonGo.GetComponent<Button>();
				button.onClick.AddListener(btnInfo.Execute);
			}

			foreach (var btnInfo in ListRmdGmLeft)
			{
				var newButtonGo = GameObject.Instantiate(m_btnServerTemp.gameObject, m_tfLeftQuickList);
				newButtonGo.SetActive(true);

				var buttonText = FindChildComponent<Text>(newButtonGo.transform, "Text");
				buttonText.text = btnInfo.GmName;
				var button = newButtonGo.GetComponent<Button>();
				button.onClick.AddListener(btnInfo.Execute);
			}

			foreach (var btnInfo in ListRmdGmOther)
			{
				var newButtonGo = GameObject.Instantiate(m_btnOtherTemp.gameObject, m_tfOtherQuickList);
				newButtonGo.SetActive(true);

				var buttonText = FindChildComponent<Text>(newButtonGo.transform, "Text");
				buttonText.text = btnInfo.GmName;
				var button = newButtonGo.GetComponent<Button>();
				button.onClick.AddListener(btnInfo.Execute);
			}
		}

		private void ExecuteCfgGM(string strGM, GmConfig cfg)
		{
			if (cfg != null)
			{
				ClearGmPanel();

				m_inputGm.text = cfg.GmOrder;

				if (cfg.OrderID > 0)
				{
					m_inputGmID.text = cfg.OrderID.ToString();
				}

				if (cfg.Num > 0)
				{
					m_inputGmNum.text = cfg.Num.ToString();
				}

				m_inputGmExecute.text = "1";

				//直接执行
				if (cfg.ExecuteDirectly)
				{
					var saveGm = new CacheGm();
					saveGm.CacheCfg.GmOrder = m_inputGm.text;
					saveGm.CacheCfg.GmType = cfg.GmType;
					saveGm.CacheCfg.ExecuteClose = cfg.ExecuteClose;
					saveGm.BatchNum = int.Parse(m_inputGmExecute.text);
					OnExecuteCfgGM(0, saveGm);
				}
				else
				{
					if (cfg.AssConfig > 0)
					{
						m_dropDownSelect.gameObject.SetActive(true);
						m_dropDownSelect.ClearOptions();
						var optionsList = new List<Dropdown.OptionData>();

						// switch ((ConfigType)cfg.AssConfig)
						// {
							// case ConfigType.Currency:
							// 	for (int i = 0; i <= (int)CurrencyCode.RandomHeroSkin; i++)
							// 	{
							// 		var currency = (CurrencyCode)i;
							// 		Dropdown.OptionData optionData = new Dropdown.OptionData();
							// 		optionData.text = $"{i} - {currency.ToString()}";
							// 		optionsList.Add(optionData);
							// 	}
							//
							// 	break;
						// }

						m_dropDownSelect.AddOptions(optionsList);
						OnDropdownSelect(0);
						m_dropDownSelect.RefreshShownValue();
					}
					else
					{
						m_dropDownSelect.ClearOptions();
						m_dropDownSelect.gameObject.SetActive(false);
					}
				}
			}
		}

		private void OnExecuteCfgGM(int executeNum, CacheGm cacheGm, bool forceClose = true)
		{
			var curOrderStr = m_inputGm.text;

			if (!string.IsNullOrEmpty(m_inputGmID.text))
			{
				int.TryParse(m_inputGmID.text, out var inputFieldID);
				cacheGm.CacheCfg.OrderID = inputFieldID;
				curOrderStr += " " + inputFieldID;
			}

			if (!string.IsNullOrEmpty(m_inputGmNum.text))
			{
				uint.TryParse(m_inputGmNum.text, out var inputNum);
				cacheGm.CacheCfg.Num = (int)inputNum;
				curOrderStr += " " + inputNum;
			}

			if (cacheGm.CacheCfg.GmType == 1)
			{
				//客户端的GM
				ClientGM.Instance.HandleClientGm(curOrderStr);
			}
			else
			{
				// 服务器GM预留
			}

			m_gmCfgCommendIndex = 0;

			if (executeNum == 0)
			{
				// 提取GM命令并且存储
				ClientGM.Instance.AddCommendCfg(cacheGm);

				ClearGmPanel();

				if (cacheGm.CacheCfg.ExecuteClose && forceClose)
				{
					Close();
				}
			}
		}

		private GmConfig GetGmCommandConfigByOrder(string order)
		{
			foreach (var item in m_gmConfigs)
			{
				if (item.GmOrder == order)
				{
					return item;
				}
			}

			return null;
		}

		private void ShowText(string text)
		{
			var allTex = m_textDisplay.text;
			allTex += "\r\n";
			allTex += text;

			m_textDisplay.text = allTex;
		}

		private void ClearGmPanel()
		{
			m_dropDownSelect.ClearOptions();
			m_dropDownSelect.gameObject.SetActive(false);
			// m_gmCommendIndex = 0;

			m_inputGm.text = "";
			m_inputGmExecute.text = "";
			m_inputGmNum.text = "";
			m_inputGmID.text = "";
		}

		#endregion

		#region 事件

		void OnDropdownSelect(int val)
		{
			string[] inputDrop = m_dropDownSelect.captionText.text.Split(' ');
			m_inputGmID.text =  inputDrop[0];
		}

		private void OnClickLastExecuteBtn()
		{
			ClearGmPanel();

			if (ClientGM.Instance.GetCommendCfgByIndex(m_gmCfgCommendIndex, out var cfg))
			{
				++m_gmCfgCommendIndex;
			}
			else
			{
				m_gmCfgCommendIndex = cfg == null ? 0 : 1;
			}

			if (cfg != null)
			{
				m_inputGm.text = cfg.CacheCfg.GmOrder;
				if (cfg.CacheCfg.OrderID > 0)
				{
					m_inputGmID.text = cfg.CacheCfg.OrderID.ToString();
				}
				if (cfg.CacheCfg.Num > 0)
				{
					m_inputGmNum.text = cfg.CacheCfg.Num.ToString();
				}
				m_inputGmExecute.text = cfg.BatchNum.ToString();
			}
		}

		private void OnClickExecuteNoCloseBtn()
		{
			OnClickExecute(false);
		}

		private void OnClickCloseBtn()
		{
			Close();
		}

		private void OnClickExecuteBtn()
		{
			OnClickExecute();
		}

		private void OnClickExecute(bool forceClose = true)
		{
			if (!string.IsNullOrEmpty(m_inputGmExecute.text))
			{
				var executeNum = int.Parse(m_inputGmExecute.text);

				var cfg = GetGmCommandConfigByOrder(m_inputGm.text);
				if (cfg == null)
				{
					DLogger.Error("请输入正确的GM命令！");
					return;
				}

				var saveGm = new CacheGm();
				saveGm.CacheCfg.GmOrder = m_inputGm.text;
				saveGm.CacheCfg.GmType = cfg.GmType;
				saveGm.CacheCfg.ExecuteClose = cfg.ExecuteClose;
				saveGm.BatchNum = executeNum;
				if (executeNum > 0 && executeNum <= 10)
				{
					for (int i = 0; i < executeNum; i++)
					{
						OnExecuteCfgGM(executeNum - (i + 1), saveGm, forceClose);
					}
				}
			}
		}

		private void OnClickServerTempBtn()
		{
		}

		private void OnClickClientTempBtn()
		{
		}

		private void OnClickBatchTempBtn()
		{
		}

		private void OnClickOtherTempBtn()
		{
		}

		#endregion
	}
}