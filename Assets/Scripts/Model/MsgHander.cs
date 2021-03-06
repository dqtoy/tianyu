﻿
//=======================================
//作者：吴江
//日期：2015/5/20
//用途：网络消息收听处理
//========================================



using System;
using System.Collections.Generic;

using System.Text;
using st.net.NetBase;
using UnityEngine;

public class MsgHander
{

    #region 命令头定义

    #region C2S
    public static ushort PT_BACK_PLY_LOGIN = 0x2062;//返回登录状态

    #endregion

    #region S2C
    //登录系统
    /// <summary>
    /// 获取角色列表
    /// </summary>
    public static ushort GET_CHARACTER_LIST = 0x3003;//获取角色列表
    #endregion
    #endregion



    /// <summary>
    /// 是否在屏幕上打印服务端消息 by吴江
    /// </summary>
    public static bool screen_Debug_Log_S2C = false;

    /// <summary>
    /// 是否在编辑器上打印服务端消息 by吴江
    /// </summary>
    public static bool editor_Debug_Log_S2C = false;



    public void Init()
    {
        msgHandDictionary = new Dictionary<int, List<System.Action<Pt>>>();
        unHandMsgList = new Queue<byte[]>();
        screen_Debug_Log_S2C = GameCenter.instance.screen_Debug_Log_S2C;
        editor_Debug_Log_S2C = GameCenter.instance.editor_Debug_Log_S2C;
        RegistAll();
    }


    protected static Dictionary<int, List<System.Action<Pt>>> msgHandDictionary;
    protected static Dictionary<int, Pt> msgStructDic = new Dictionary<int, Pt>();

    protected static Queue<byte[]> unHandMsgList;

    public static void Regist(int _head, Action<Pt> _action)
    {
        if (!msgStructDic.ContainsKey(_head))
        {
            Debug.Log("编号为: " + _head + "的协议未在MsgHander中声明，无法注册事件！");
        }
        if (!msgHandDictionary.ContainsKey(_head))
        {
            msgHandDictionary[_head] = new List<Action<Pt>>();
        }
        msgHandDictionary[_head].Add(_action);
    }


    public static void UnRegist(int _head, Action<Pt> _action)
    {
        if (msgHandDictionary.ContainsKey(_head) && msgHandDictionary[_head].Contains(_action))
        {
            msgHandDictionary[_head].Remove(_action);
        }
    }



    public static void ProcComand(Pt _pt)
    {
        uint serializeID = _pt.seq;//先取出问答序列号 by吴江
        if (serializeID != 0 && serializeID < 1000000)
        {
             GameCenter.msgLoackingMng.UpdateSerializeList((int)serializeID, false);
        }
        int id = _pt.GetID();
        if (msgHandDictionary.ContainsKey(id) && msgHandDictionary[id] != null)
        {
            List<System.Action<Pt>> list = msgHandDictionary[id];
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                list[i](_pt);
            }
        }
        else
            GameSys.LogError("命令头为" + id + " ,十进制：" + Convert.ToString(id, 16) + "的消息没有接收者！");
    }



    public static void PutCmd(byte[] _bytes)
    {
        lock (unHandMsgList)
        {
            unHandMsgList.Enqueue(_bytes);
        }
    }

    public static void Update()
    {

        if (unHandMsgList != null)
        {
            while (unHandMsgList.Count > 0)
            {
                Pt pt = null;
                if (unHandMsgList.Count > 0)
                {
                    lock (unHandMsgList)
                    {
                        byte[] bts = unHandMsgList.Dequeue();
                        byte[] bytes_cmdid = new byte[2];
                        byte[] bytes_seq = new byte[4];
                        byte[] bytes_body = new byte[bts.Length - 6];
                        ByteAr_b.PickBytes(bts, bytes_cmdid.Length, bytes_cmdid, 0);
                        ByteAr_b.PickBytes(bts, bytes_seq.Length, bytes_seq, 2);
                        ByteAr_b.PickBytes(bts, bytes_body.Length, bytes_body, bytes_cmdid.Length + bytes_seq.Length);

                        ushort head = ByteAr_b.b2us(bytes_cmdid);
                        if (msgStructDic.ContainsKey(head))
                        {
                            pt = msgStructDic[head].createNew();
                            pt.seq = ByteAr_b.b2ui(bytes_seq);
                            pt.fromBinary(bytes_body);

                        }
                    }
                }

                if (pt != null)
                {
                    if (screen_Debug_Log_S2C)
                    {
                        GameSys.LogInternal("[S] to [C] :" + Convert.ToString(pt.GetID(), 16), false);
                    }
                    if (editor_Debug_Log_S2C)
                    {
                        Debug.Log("[S] to [C] :" + Convert.ToString(pt.GetID(), 16));
                    }
                    MsgHander.ProcComand(pt);
                }
            }
        }
    }


    protected void RegistAll()
    {
        msgStructDic[0xA001] = new pt_login_a001();
        msgStructDic[0xA102] = new pt_usr_list_a102();
        msgStructDic[0xA003] = new pt_req_net_a003();
        msgStructDic[0xA104] = new pt_queue_info_a104();
        msgStructDic[0xA105] = new pt_net_info_a105();
        msgStructDic[0xA006] = new pt_req_create_usr_a006();
        msgStructDic[0xA107] = new pt_create_usr_list_a107();
        msgStructDic[0xA008] = new pt_req_delete_usr_a008();
        msgStructDic[0xA109] = new pt_delete_usr_a109();
        msgStructDic[0xA10A] = new pt_ping_a10a();




        msgStructDic[0xB001] = new pt_usr_enter_b001();
        msgStructDic[0xB003] = new pt_usr_enter_scene_b003();
        msgStructDic[0xB005] = new pt_load_scene_finish_b005();
        msgStructDic[0xB102] = new pt_usr_info_b102();
        msgStructDic[0xB104] = new pt_req_load_scene_b104();
        msgStructDic[0xB106] = new pt_scene_info_b106();

        msgStructDic[0xC001] = new pt_scene_move_c001();
        msgStructDic[0xC002] = new pt_scene_add_c002();
		msgStructDic[0xD810] = new pt_other_ply_info_d810();
        msgStructDic[0xC003] = new pt_scene_dec_c003();

        msgStructDic[0xC006] = new pt_scene_skill_effect_c006();
        msgStructDic[0xC008] = new pt_scene_chg_buff_c008();
        msgStructDic[0xC009] = new pt_scene_remove_buff_c009();
        msgStructDic[0xC011] = new pt_scene_drop_c011();
        msgStructDic[0xC012] = new pt_scene_pickup_c012();
        msgStructDic[0xC013] = new pt_scene_transform_c013();
        msgStructDic[0xC014] = new pt_scene_add_arrow_c014();
        msgStructDic[0xC015] = new pt_scene_add_trap_c015();
        msgStructDic[0xC016] = new pt_scene_delete_arrow_c016();
        msgStructDic[0xC017] = new pt_scene_delete_trap_c017();
        msgStructDic[0xC018] = new pt_scene_usr_team_chg_c018();
        msgStructDic[0xC020] = new pt_scene_hide_c020();
        msgStructDic[0xC021] = new pt_scene_skill_aleret_c021();
        msgStructDic[0xC022] = new pt_scene_skill_aleret_cancel_c022();
        msgStructDic[0xC01A] = new pt_req_jump_c01a();
        msgStructDic[0xC01B] = new pt_monster_affiliation_c01b();
        msgStructDic[0xC117] = new pt_req_get_offline_reward_c117();
        msgStructDic[0xC118] = new pt_offline_reward_info_c118();
        msgStructDic[0xC120] = new pt_surround_task_fly_state_c120();
        msgStructDic[0xC122] = new pt_update_new_name_c122();
        msgStructDic[0xC128] = new pt_tarot_info_c128();
        msgStructDic[0xC129] = new pt_tarot_reward_list_c129();
        msgStructDic[0xC131] = new pt_seven_day_target_rewards_info_c131();
        msgStructDic[0xC132] = new pt_seven_day_single_target_c132();
        msgStructDic[0xC143] = new pt_update_task_surround_ui_info_c143();
        msgStructDic[0xC144] = new pt_update_quell_demon_star_c144();
        msgStructDic[0xC145] = new pt_update_quell_demon_drop_out_c145();
        msgStructDic[0xC147] = new pt_quell_demon_info_c147();
        msgStructDic[0xC149] = new pt_mountain_of_flames_count_time_c149();
        msgStructDic[0xD001] = new pt_error_info_d001();
        msgStructDic[0xD002] = new pt_action_d002();
        msgStructDic[0xD003] = new pt_action_int_d003();
        msgStructDic[0xD004] = new pt_action_string_d004();

        msgStructDic[0xD007] = new pt_item_info_d007();
        msgStructDic[0xD008] = new pt_item_chg_d008();

        msgStructDic[0xD009] = new pt_lost_item_d009();
		msgStructDic[0xD013] = new pt_task_list_d013();
		msgStructDic[0xD014] = new pt_update_task_d014();
        msgStructDic[0xD016] = new pt_finish_task_d016();
		msgStructDic[0xD017] = new pt_del_task_d017();
		msgStructDic[0xD018] = new pt_accept_task_response_d018();

        msgStructDic[0xD01A] = new pt_lost_item_get_d01a();
        msgStructDic[0xD01B] = new pt_lost_item_lev_d01b();
        msgStructDic[0xD01E] = new pt_lost_item_active_d01e();
        msgStructDic[0xD020] = new pt_mastery_d020();
        msgStructDic[0xD021] = new pt_mastery_update_d021();
        msgStructDic[0xD80B] = new pt_recharge_flag_d80b();

        msgStructDic[0xD01F] = new pt_entourage_create_d01f();
        msgStructDic[0xD010] = new pt_entourage_list_d010();
        msgStructDic[0xD011] = new pt_entourage_info_d011();
        msgStructDic[0xD012] = new pt_action_two_int_d012();
        msgStructDic[0xD822] = new pt_download_reward_result_d822();
        msgStructDic[0xD791] = new pt_update_companion_copy_id_d791();
        msgStructDic[0xD792] = new pt_updata_poetry_d792();
        msgStructDic[0xD797] = new pt_give_flower_allserver_inform_d797();
        msgStructDic[0xD981] = new pt_reply_login_dividend_info_d981();
        msgStructDic[0xD977] = new pt_reply_treasure_lottery_d977();
        msgStructDic[0xD990] = new pt_daily_recharge_benifit_info_d990();
        msgStructDic[0xD991] = new pt_daily_recharge_benifit_succ_d991();

        msgStructDic[0xD01C] = new pt_req_task_list_d01c();
        msgStructDic[0xD01D] = new pt_update_base_d01d();
        msgStructDic[0xD015] = new pt_accept_task_d015();
        msgStructDic[0xD016] = new pt_finish_task_d016();
        msgStructDic[0xD019] = new pt_finish_task_response_d019();
        //队伍信息
        msgStructDic[0xD022] = new pt_team_info_d022();
        //队伍成员改变信息
        msgStructDic[0xD023] = new pt_team_member_chg_d023();
        //邀请组队
        msgStructDic[0xD024] = new pt_team_ask_d024();
        //回答邀请
        msgStructDic[0xD025] = new pt_team_ans_ask_d025();
        //申请组队
        msgStructDic[0xD026] = new pt_team_req_d026();
        //回答申请
        msgStructDic[0xD027] = new pt_team_ans_req_d027();
        //队长改变
        msgStructDic[0xD028] = new pt_team_leader_chg_d028();
        //有成员离开队伍
        msgStructDic[0xD029] = new pt_team_member_leave_d029();
        //队伍解散
        msgStructDic[0xD030] = new pt_team_destroy_d030();
        //邀请取消
        msgStructDic[0xD031] = new pt_team_ask_cancle_d031();
        //申请取消
        msgStructDic[0xD032] = new pt_team_req_cancle_d032();
        //商城
        msgStructDic[0xD033] = new pt_store_info_d033();
        //商城购买
        msgStructDic[0xD034] = new pt_store_buy_d034();
        
        msgStructDic[0xD100] = new pt_all_skill_d100();
        msgStructDic[0xD101] = new pt_reloading_d101();

        msgStructDic[0xD103] = new pt_return_gem_update_d103();

        msgStructDic[0xD105] = new pt_return_sign_d105();
        msgStructDic[0xD106] = new pt_activity_info_d106();
       

        // 好友列表
        msgStructDic[0xD107] = new pt_friends_info_d107();

        msgStructDic[0xD108] = new pt_open_box_get_item_d108();
        // 好友推荐列表
        msgStructDic[0xD109] = new pt_recommend_friends_list_d109();

        msgStructDic[0xD110] = new pt_get_success_d110();
        msgStructDic[0xD111] = new pt_all_guild_list_info_d111();
        msgStructDic[0xD112] = new pt_guild_member_info_d112();
        msgStructDic[0xD113] = new pt_members_entry_d113();
        msgStructDic[0xD114] = new pt_guild_commonality_d114();
        msgStructDic[0xD115] = new pt_guild_succeed_d115();


        msgStructDic[0xD11a] = new pt_guild_building_list_d11a();
        msgStructDic[0xD11b] = new pt_donation_record_list_d11b();
        msgStructDic[0xD11c] = new pt_guild_copy_list_d11c();
        msgStructDic[0xD11d] = new pt_guild_copy_trophy_d11d();
        msgStructDic[0xD11e] = new pt_guild_copy_enter_d11e();
        msgStructDic[0xD11f] = new pt_guild_copy_damage_ranking_d11f();
        
        
        
        msgStructDic[0xD117] = new pt_item_recoin_return_d117();
        msgStructDic[0xD118] = new pt_entourage_succeed_d118();


        msgStructDic[0xD120] = new pt_ranklist_d120();

        msgStructDic[0xD121] = new pt_receive_rewards_succeed_d121();
        msgStructDic[0xD122] = new pt_rewards_return_d122();

        msgStructDic[0xD123] = new pt_usr_info_equi_d123();
        msgStructDic[0xD124] = new pt_usr_info_property_d124();
        msgStructDic[0xD125] = new pt_usr_info_entourage_d125();
        msgStructDic[0xD126] = new pt_usr_info_lost_item_d126();
        msgStructDic[0xD127] = new pt_draw_d127();
        msgStructDic[0xD128] = new pt_draw_times_d128();
        msgStructDic[0xD129] = new pt_item_info_return_d129();

		msgStructDic[0xD130] = new pt_bags_chg_d130();
		msgStructDic[0xD131] = new pt_add_bags_d131();

        msgStructDic[0xD132] = new pt_wanted_task_d132();
        msgStructDic[0xD133] = new pt_task_rewards_succeed_d133();
        msgStructDic[0xD135] = new pt_camp_task_d135();
        msgStructDic[0xD136] = new pt_item_model_d136();
        msgStructDic[0xD137] = new pt_guild_name_d137();
        msgStructDic[0xD138] = new pt_guild_notice_d138();
        msgStructDic[0xD139] = new pt_chapter_info_d139();


        msgStructDic[0xD140] = new pt_backpack_upgrade_d140();

        msgStructDic[0xD201] = new pt_pet_d201();
        msgStructDic[0xD202] = new pt_say_notify_d202();
        msgStructDic[0xD203] = new pt_progress_bar_begin_d203();
        msgStructDic[0xD204] = new pt_progress_bar_end_d204();
        msgStructDic[0xD205] = new pt_revive_d205();
        msgStructDic[0xD206] = new pt_copy_times_d206();

        msgStructDic[0xD207] = new pt_revive_times_d207();
        msgStructDic[0xD208] = new pt_chat_d208();
        msgStructDic[0xD211] = new pt_update_mail_d211();
        msgStructDic[0xD212] = new pt_start_timer_d212();
        msgStructDic[0xD213] = new pt_break_timer_d213();

        msgStructDic[0xD215] = new pt_stop_match_d215();
        msgStructDic[0xD216] = new pt_match_ready_cancel_d216();
        msgStructDic[0xD217] = new pt_start_match_d217();
        msgStructDic[0xD218] = new pt_match_succ_d218();
        msgStructDic[0xD219] = new pt_submit_ready_d219();
        msgStructDic[0xD222] = new pt_update_scene_usr_data_d222();
        msgStructDic[0xD223] = new pt_copy_exist_time_d223();
        msgStructDic[0xD224] = new pt_crowd_num_d224();
        msgStructDic[0xD20A] = new pt_req_mail_d20a();
        msgStructDic[0xD20B] = new pt_scene_jump_d20b();
        msgStructDic[0xD20C] = new pt_mail_content_d20c();
        msgStructDic[0xD20D] = new pt_del_mail_d20d();

        msgStructDic[0xD220] = new pt_military_prize_d220();
        msgStructDic[0xD221] = new pt_camp_activity_d221();

        msgStructDic[0xD20E] = new pt_copy_win_d20e();

        msgStructDic[0xD21A] = new pt_camp_vote_data_d21a();
        msgStructDic[0xD21C] = new pt_join_camp_d21c();
        //msgStructDic[0xD21D] = new pt_update_camp_d21d();
        msgStructDic[0xD21E] = new pt_can_worship_d21e();
        msgStructDic[0xD21F] = new pt_worship_response_d21f();

        msgStructDic[0xD402] = new pt_pet_list_d402();
        msgStructDic[0xD404] = new pt_pet_updata_state_d404();
        msgStructDic[0xD409] = new pt_pet_updata_property_d409();
        msgStructDic[0xD410] = new pt_fuse_info_d410();
        msgStructDic[0xD426] = new pt_update_pet_name_d426();
        msgStructDic[0xD439] = new pt_update_ride_lev_d439();
        msgStructDic[0xD440] = new pt_update_ride_state_d440();
        
		msgStructDic[0xD302] = new pt_decompose_result_d302();
		msgStructDic[0xD319] = new pt_store_house_info_d319();
		msgStructDic[0xD322] = new pt_item_chg_d322();
		msgStructDic[0xD323] = new pt_store_house_item_chg_d323();
		msgStructDic[0xD351] = new pt_equ_info_d351();
        msgStructDic[0xD788] = new pt_update_model_lev_exp_d788();
        msgStructDic[0xD531] = new pt_req_break_marry_d531();
        msgStructDic[0xC111] = new pt_mountain_flames_win_c111();
        msgStructDic[0xC109] = new pt_update_mountain_flames_rank_c109();
        msgStructDic[0xC110] = new pt_update_mountain_flames_score_c110();
        msgStructDic[0xC113] = new pt_update_back_city_time_c113();
        msgStructDic[0xC114] = new pt_update_general_state_c114();
        msgStructDic[0xC115] = new pt_update_battleground_id_c115();
        msgStructDic[0xD961] = new pt_lucky_wheel_info_d961();
        msgStructDic[0xD963] = new pt_lucky_wheel_record_d963();
        msgStructDic[0xD965] = new pt_lucky_wheel_reward_d965();

        #region ljq
        msgStructDic[0xD309] = new pt_magic_weapons_state_d309();
        //msgStructDic[0xD310] = new pt_req_magic_weapons_state_d310();
        msgStructDic[0xD417] = new pt_wing_list_info_d417();
        msgStructDic[0xD420] = new pt_update_wing_lev_d420();
        msgStructDic[0xD443] = new pt_update_wing_state_d443();
        msgStructDic[0xD378] = new pt_treasure_d378();
        msgStructDic[0xD390] = new pt_treasure_house_info_d390();
        msgStructDic[0xD391] = new pt_treasure_item_info_d391();
        msgStructDic[0xD511] = new pt_treasure_record_d511();
        msgStructDic[0xD601] = new pt_ranklist_d601();
        msgStructDic[0xC010] = new pt_update_monster_owner_c010();
        msgStructDic[0xF001] = new pt_ret_sevenDayRewardsInfo_f001();
        msgStructDic[0xF002] = new pt_ret_firstChargeRewardInfo_f002();
        msgStructDic[0xD766] = new pt_update_achievement_reach_num_d766();
        msgStructDic[0xD767] = new pt_update_achievement_reward_d767();
        msgStructDic[0xD773] = new pt_req_look_rank_usrinfo_d773();
        msgStructDic[0xD776] = new pt_update_red_dot_d776();
        msgStructDic[0xC00B] = new pt_passive_skill_effect_c00b();
        msgStructDic[0xD778] = new pt_guild_storm_city_over_d778();
        msgStructDic[0xD783] = new pt_achievement_red_dot_list_d783();
        msgStructDic[0xD815] = new pt_bind_result_d815();
        msgStructDic[0xD794] = new pt_update_jewelry_list_d794();
        msgStructDic[0xD796] = new pt_gather_jewelry_finish_d796();
        msgStructDic[0xD941] = new pt_reply_royal_box_list_d941();
        msgStructDic[0xD950] = new pt_hidden_task_info_d950();
        msgStructDic[0xC105] = new pt_update_function_start_reward_c105();
        msgStructDic[0xC133] = new pt_update_second_recharge_reward_c133();
        #endregion

        msgStructDic[0xD540] = new pt_wsorn_brother_info_d540();
        msgStructDic[0xD534] = new pt_keepsake_info_d534();
        msgStructDic[0xD543] = new pt_brother_reward_info_d543();
        msgStructDic[0xD547] = new pt_req_break_brother_d547();
        msgStructDic[0xD591] = new pt_companion_d591();
        msgStructDic[0xD705] = new pt_friend_relation_list_d705();
        msgStructDic[0xD710] = new pt_update_find_friend_d710();
        msgStructDic[0xD751] = new pt_rand_box_reward_d751(); 
        msgStructDic[0xD743] = new pt_everyday_reward_list_d743();
        msgStructDic[0xD757] = new pt_update_ride_skin_lev_d757();
        msgStructDic[0xD759] = new pt_update_friend_intimacy_d759();
        msgStructDic[0xD762] = new pt_update_lev_reward_d762();
        msgStructDic[0xD806] = new pt_love_reward_d806();
        msgStructDic[0xD921] = new pt_reply_weekcard_info_d921();
        msgStructDic[0xD923] = new pt_reply_get_weekcard_reward_d923();
        msgStructDic[0xD690] = new pt_sworn_success_d690();
        msgStructDic[0xD808] = new pt_love_reward_list_d808();
        //msgStructDic[0xD688] = new pt_marry_success_d688();

        msgStructDic[0xE10A] = new pt_acquire_new_title_e10a();
        msgStructDic[0xE10B] = new pt_ret_titles_e10b();
        msgStructDic[0xE10C] = new pt_use_someone_title_e10c();
        msgStructDic[0xE10D] = new pt_update_usr_discrib_e10d();

        msgStructDic[0xE110] = new pt_ret_scene_teams_e110();

        msgStructDic[0xE111] = new pt_ret_ride_info_e111();
        msgStructDic[0xE112] = new pt_ret_ride_prop_e112();
        msgStructDic[0xE113] = new pt_ret_skin_list_e113();
        msgStructDic[0xE114] = new pt_broadcast_ride_info_e114();
        msgStructDic[0xE115] = new pt_ret_trials_info_e115();

        msgStructDic[0xE117] = new pt_ret_hero_challenge_e117();
        msgStructDic[0xE118] = new pt_req_continue_hc_e118();


        #region hhx
        msgStructDic[0xD400] = new pt_change_skill_d400 ();
		msgStructDic[0xD401] = new pt_use_skill_list_d401 ();
		msgStructDic[0xD325] = new pt_chat_content_d325 ();
		msgStructDic[0xD412] = new pt_model_clothes_list_d412 ();
		msgStructDic[0xD414] = new pt_updata_model_clothes_d414 ();
		msgStructDic[0xD422] = new pt_title_list_d422 ();
		msgStructDic[0xD423] = new pt_update_title_d423 ();
		msgStructDic[0xD446] = new pt_cast_soul_info_d446 ();
		msgStructDic[0xD447] = new pt_update_cast_soul_num_d447 ();
		msgStructDic[0xD372] = new pt_buy_items_d372 ();
		msgStructDic[0xD373] = new pt_shop_items_d373 ();
		msgStructDic[0xD514] = new pt_guild_skill_list_d514 ();
        msgStructDic[0xD550] = new pt_shelve_items_info_d550();
        msgStructDic[0xB107] = new pt_scene_tele_b107();
        msgStructDic[0xD717] = new pt_update_budo_match_info_d717();
        msgStructDic[0xD720] = new pt_update_budo_log_list_d720();
        //msgStructDic[0xD601] = new pt_ranklist_d601();
        msgStructDic[0xD721] = new pt_update_budo_apply_d721();
        msgStructDic[0xD555] = new pt_shelve_item_chg_d555();
        msgStructDic[0xD556] = new pt_guild_battle_integer_d556();
        msgStructDic[0xD557] = new pt_guild_battle_info_ex_d557();
        msgStructDic[0xD558] = new pt_guild_battle_index_d558();
        msgStructDic[0xD736] = new pt_budo_win_d736();
        msgStructDic[0xD737] = new pt_usr_die_info_update_d737();
        msgStructDic[0xD741] = new pt_other_guild_bonfire_info_d741();
        msgStructDic[0xD631] = new pt_req_trade_s2c_d631();
        msgStructDic[0xD633] = new pt_trade_start_d633();
        msgStructDic[0xD635] = new pt_reply_lock_trade_d635();
        msgStructDic[0xD639] = new pt_trade_confirm_d639();
        msgStructDic[0xD638] = new pt_trade_finish_d638();
        msgStructDic[0xD695] = new pt_guild_battle_rest_time_d695();
        msgStructDic[0xD694] = new pt_guild_battle_state_d694();
        msgStructDic[0xD763] = new pt_update_online_reward_d763();
        msgStructDic[0xD901] = new pt_reply_all_operation_activity_d901();
        msgStructDic[0xD903] = new pt_reply_operation_activity_info_d903();
        msgStructDic[0xD905] = new pt_reply_operation_activity_reward_d905();
        msgStructDic[0xD911] = new pt_reply_open_server_gift_info_d911();
        msgStructDic[0xD913] = new pt_reply_buy_open_server_gift_d913();
        msgStructDic[0xD689] = new pt_system_msg_d689();
        msgStructDic[0xD777] = new pt_cast_soul_crit_d777();
        msgStructDic[0xC101] = new pt_update_cast_soul_reward_c101();
        msgStructDic[0xC107] = new pt_online_choujiang_id_c107();
        msgStructDic[0xC119] = new pt_update_little_window_c119();

        #endregion



        #region 何明军
        msgStructDic[0xD329] = new pt_vip_info_d329();
		msgStructDic[0xD442] = new pt_update_fly_up_num_d442();
		msgStructDic[0xD339] = new pt_all_mail_list_d339();
		msgStructDic[0xD335] = new pt_del_mail_d335();
		msgStructDic[0xD337] = new pt_mail_info_list_d337();
		msgStructDic[0xD340] = new pt_req_send_mail_d340 ();
		msgStructDic[0xD435] = new pt_endless_pass_list_d435();
		msgStructDic[0xD437] = new pt_stra_reward_list_d437();
		msgStructDic[0xD449] = new pt_update_endless_pass_d449();
		msgStructDic[0xD451] = new pt_copy_sweep_list_d451();
		msgStructDic[0xD453] = new pt_single_many_copy_info_d453();
		msgStructDic[0xD455] = new pt_update_single_num_d455();
		msgStructDic[0xD457] = new pt_uptate_single_many_star_d457();
		msgStructDic[0xD460] = new pt_many_copy__member_challengenum_d460();
		msgStructDic[0xD465] = new pt_update_quit_many_copy_ui_d465();
		msgStructDic[0xD463] = new pt_update_many_copy_difficulty_d463();
		msgStructDic[0xD466] = new pt_update_prepare_state_d466();
		msgStructDic[0xD467] = new pt_win_list_d467();
		msgStructDic[0xD469] = new pt_lucky_brand_list_d469();
		msgStructDic[0xD470] = new pt_copy_loser_d470();
        msgStructDic[0xD473] = new pt_fly_up_xiuxing_info_d473();
        msgStructDic[0xD475] = new pt_update_fly_up_lev_d475();
        msgStructDic[0xD477] = new pt_update_yunqi_tuan_list_d477();
		msgStructDic[0xD485] = new pt_pk_info_d485();
		msgStructDic[0xD491] = new pt_pk_win_d491();
		msgStructDic[0xD489] = new pt_update_rank_reward_d489();
		msgStructDic[0xD47a] = new pt_copy_pause_d47a();
		msgStructDic[0xD715] = new pt_update_activity_info_d715();
		msgStructDic[0xD716] = new pt_quell_demon_win_d716();
		msgStructDic[0xB105] = new pt_sync_time_b105();
		msgStructDic[0xD746] = new pt_update_recruit_robot_list_d746();
		msgStructDic[0xD749] = new pt_ans_recruit_friend_d749();
		msgStructDic[0xD753] = new pt_copy_pick_item_time_d753();
		msgStructDic[0xD756] = new pt_activity_guild_guard_time_d756();
		msgStructDic[0xD313] = new pt_compose_result_d313();
		msgStructDic[0xD771] = new pt_update_recruit_robot_to_member_d771();
		msgStructDic[0xD802] = new pt_system_notic_d802();
		msgStructDic[0xD804] = new pt_new_function_open_aready_d804();
		msgStructDic[0xD779] = new pt_update_guidance_d779();
		msgStructDic[0xD786] = new pt_update_server_starttime_d786();
		#endregion

		#region dc
		msgStructDic[0xA002] = new pt_login_data_a002();
		msgStructDic[0xA004] = new pt_login_failed_a004();
		msgStructDic[0xC00A] = new pt_scene_route_c00a();
        msgStructDic[0xC103] = new pt_update_liveness_reward_c103();
        msgStructDic[0xC124] = new pt_return_guild_ask_c124();
        msgStructDic[0xC135] = new pt_on_hook_ui_info_c135();
        msgStructDic[0xC136] = new pt_update_on_hook_c136();
        msgStructDic[0xC138] = new pt_boss_copy_ui_info_c138();
        msgStructDic[0xC139] = new pt_update_boss_copy_c139();
		msgStructDic[0xD361] = new pt_strengthen_info_d361();
		msgStructDic[0xD364] = new pt_gem_action_result_d364();
		msgStructDic[0xD365] = new pt_spare_propertys_d365();
		msgStructDic[0xD368] = new pt_req_store_pro_to_equip_d368();
		msgStructDic[0xD369] = new pt_inhert_d369();
		msgStructDic[0xD370] = new pt_req_change_equip_d370();
		msgStructDic[0xD379] = new pt_guild_item_info_d379();
		msgStructDic[0xD380] = new pt_guild_info_d380();
		msgStructDic[0xD381] = new pt_guild_members_info_d381();
		msgStructDic[0xD382] = new pt_req_creat_guild_d382();
		msgStructDic[0xD392] = new pt_guild_log_info_d392();
		msgStructDic[0xD394] = new pt_req_guild_salary_d394();
		msgStructDic[0xD492] = new pt_update_copy_time_d492();
		msgStructDic[0xD493] = new pt_update_monster_wave_num_d493();
		msgStructDic[0xD495] = new pt_update_call_boss_d495();
		msgStructDic[0xD496] = new pt_update_monster_die_num_d496();
		msgStructDic[0xD497] = new pt_update_copy_tier_d497();
		msgStructDic[0xD498] = new pt_update_ding_rescue_time_d498();
		msgStructDic[0xD499] = new pt_update_copy_revive_num_d499();
		msgStructDic[0xD501] = new pt_guild_list_d501();
		msgStructDic[0xD503] = new pt_ask_join_list_d503();
		msgStructDic[0xD506] = new pt_guild_item_chg_d506();
        msgStructDic[0xD50A] = new pt_guild_contribute_result_d50a();
        msgStructDic[0xD50C] = new pt_guild_contribute_info_d50c();
        msgStructDic[0xD50E] = new pt_guild_liveness_info_d50e();
        msgStructDic[0xD51A] = new pt_guild_liveness_reward_succ_d51a();
		msgStructDic[0xD517] = new pt_leader_req_add_all_receive_state_d517();
		msgStructDic[0xD524] = new pt_guild_check_out_item_ask_list_d524();
		msgStructDic[0xD525] = new pt_guild_items_log_d525();
		msgStructDic[0xD527] = new pt_guild_name_chg_d527();
        msgStructDic[0xD572] = new pt_mequip_strengthen_d572();
        msgStructDic[0xD573] = new pt_mequip_upgrade_d573();
        msgStructDic[0xD575] = new pt_mequip_list_reply_d575();
        msgStructDic[0xD576] = new pt_mequip_list_update_d576();
		msgStructDic[0xD611] = new pt_reply_holy_crystal_info_d611();
		msgStructDic[0xD614] = new pt_reply_cart_pos_d614();
		msgStructDic[0xD615] = new pt_reply_start_cart_escort_d615();
		msgStructDic[0xD616] = new pt_cart_escort_succ_d616();
		msgStructDic[0xD692] = new pt_liveness_state_d692();
		msgStructDic[0xD696] = new pt_update_bags_info_d696();
		msgStructDic[0xD698] = new pt_shilian_task_info_d698();
		msgStructDic[0xD700] = new pt_update_copy_boss_count_d700();
		msgStructDic[0xD701] = new pt_boss_challenge_list_d701();
		msgStructDic[0xD70A] = new pt_camera_follow_d70a();
		msgStructDic[0xD70B] = new pt_cancel_camera_follow_d70b();
		msgStructDic[0xD712] = new pt_look_usr_list_d712();
		msgStructDic[0xD713] = new pt_look_pet_ride_info_d713();
		msgStructDic[0xD723] = new pt_update_fengshen_up_down_d723();
		msgStructDic[0xD724] = new pt_update_copy_score_d724();
		//msgStructDic[0xD725] = new pt_update_die_num_d725();
		msgStructDic[0xD726] = new pt_req_guild_guard_something_d726();
		msgStructDic[0xD727] = new pt_uptate_guild_guard_rank_d727();
		msgStructDic[0xD728] = new pt_activity_game_over_d728();
		msgStructDic[0xD730] = new pt_guild_storm_city_ui_info_d730();
		msgStructDic[0xD731] = new pt_guild_storm_city_apply_list_d731();
		msgStructDic[0xD733] = new pt_update_guild_astrict_list_d733();
		msgStructDic[0xD734] = new pt_update_die_num_d734();
		msgStructDic[0xD768] = new pt_update_bonfire_exp_d768();
		msgStructDic[0xD769] = new pt_update_jingshi_hp_d769();
		msgStructDic[0xD770] = new pt_update_jingshi_exp_addition_d770();
		msgStructDic[0xD772] = new pt_update_quell_demon_tier_reward_d772();
		msgStructDic[0xD780] = new pt_update_guard_hp_d780();
		msgStructDic[0xD781] = new pt_guild_storm_city_portal_d781();
		msgStructDic[0xD782] = new pt_guild_storm_city_shuijing_d782();
		msgStructDic[0xD789] = new pt_wrath_of_heaven_d789();
		msgStructDic[0xDA01] = new pt_reply_order_da01();
        msgStructDic[0xD80A] = new pt_recharge_succ_d80a();
		msgStructDic[0xD819] = new pt_recharge_benefit_result_d819();
        #endregion
        #region 唐源
        msgStructDic[0xD944] = new pt_add_royal_box_d944();
        msgStructDic[0xD971] = new pt_reply_treasure_info_d971();
        msgStructDic[0xD973] = new pt_reply_treasure_rank_reward_info_d973();
        msgStructDic[0xD975] = new pt_reply_treasure_times_reward_info_d975();
        msgStructDic[0xD979] = new pt_treasure_base_info_d979();
        #endregion
    }
}

