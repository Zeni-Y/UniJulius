#include "pch.h"
#include <stdio.h>
#include <stdlib.h>

#define SUCCESS 0
#define ALREADY_DONE 1
#define NOT_BEGIN -1
#define SR_INSTANCE_NOT_FOUND -2
#define GRAMMAR_NOT_FOUND -3
#define OUTPUT_FUNCTION_IS_NOT_DEFINED -4
#define FAILED_LOAD_JCONF -5
#define FAILED_CREATE_RECOG -6
#define FAILED_CREATE_ENGINE -7
#define FAILED_OPEN_STREAM -8
#define DEVICE_ERROR -9
#define UNKNOWN_ERROR -10

static Jconf* jconf = NULL;
static Recog* recog = NULL;

static debug_log_func_str_type debug_log_str = NULL;
static debug_log_func_str_int_type debug_log_str_int = NULL;
static audio_read_callback_func_type audio_read_callback = NULL;
static result_func_type output_result = NULL;

void clean_up_if_exists()
{
	if (recog != NULL)
	{
		//jconf will be released inside this
		j_recog_free(recog);
		jconf = NULL;
		recog = NULL;
	}

	if (jconf != NULL) {
		j_jconf_free(NULL);
		jconf = NULL;
	}
}

static void on_process_online(Recog* recog, void* dummy)
{
	debug_log_str("on_process_online");
}

static void on_process_offline(Recog* recog, void* dummy)
{
	debug_log_str("on_process_offline");
}

static void on_speech_ready(Recog* recog, void* dummy)
{
	debug_log_str("on_speech_ready");
}

static void on_speech_start(Recog* recog, void* dummy)
{
	debug_log_str("on_speech_start");
}

static void on_speech_stop(Recog* recog, void* dummy)
{
	debug_log_str("on_speech_stop");
}

static void on_recognition_begin(Recog* recog, void* dummy)
{
	debug_log_str("on_recognition_begin");
}

static void on_recognition_end(Recog* recog, void* dummy)
{
	debug_log_str("on_recognition_end");
}

static void put_hypo_phoneme(WORD_ID* seq, int n, WORD_INFO* winfo)
{
	int i, j;
	WORD_ID w;
	static char buf[MAX_HMMNAME_LEN];

	if (seq != NULL) {
		for (i = 0; i < n; i++) {
			w = seq[i];
			for (j = 0; j < winfo->wlen[w]; j++) {
				center_name(winfo->wseq[w][j]->name, buf);
			}
		}
	}
}

static void on_pause(Recog* recog, void* dummy)
{
	debug_log_str("on_pause");
}

static void on_resume(Recog* recog, void* dummy)
{
	debug_log_str("on_resume");
}

static void on_pause_function(Recog* recog, void* dummy)
{
	debug_log_str("on_pause_function");
}

static void on_result(Recog* recog, void* dummy)
{
	debug_log_str("on_result");
	write_result(recog, dummy);
}

static void on_pass1_frame(Recog* recog, void* dummy)
{
	int i, j;
	int len;
	WORD_INFO* winfo;
	WORD_ID* seq;
	int seqnum;
	int n = 0;
	Sentence* s;
	RecogProcess* r;
	HMM_Logical* p;
	SentenceAlign* align;

	TRELLIS_ATOM* tre;
	TRELLIS_ATOM* tremax;
	LOGPROB maxscore;
	WORD_ID w;
	MULTIGRAM* m;

	char result_string[65535] = "";
	char temp_buffer[65535];
	strcat_s(result_string, sizeof(result_string) - 1, "on_pass1_frame\n");
	/* all recognition results are stored at each recognition process
	instance */
	for (r = recog->process_list; r; r = r->next) {

		/* skip the process if the process is not alive */
		if (!r->live) continue;

		/* result are in r->result.  See recog.h for details */

		/* check result status */
		if (r->result.status < 0) {      /* no results obtained */
										 /* outout message according to the status code */

			strcat_s(result_string, sizeof(result_string) - 1, "error\t");
			//SR instance名
			strcat_s(result_string, sizeof(result_string) - 1, r->config->name);
			strcat_s(result_string, sizeof(result_string) - 1, "\t");
			//文法名
			strcat_s(result_string, sizeof(result_string) - 1, r->lm->grammars->name);
			strcat_s(result_string, sizeof(result_string) - 1, "\n");

			
			switch (r->result.status) {
			case J_RESULT_STATUS_REJECT_POWER:
				debug_log_str("<input rejected by power>\n");
				break;
			case J_RESULT_STATUS_TERMINATE:
				debug_log_str("<input teminated by request>\n");
				break;
			case J_RESULT_STATUS_ONLY_SILENCE:
				debug_log_str("<input rejected by decoder (silence input result)>\n");
				break;
			case J_RESULT_STATUS_REJECT_GMM:
				debug_log_str("<input rejected by GMM>\n");
				break;
			case J_RESULT_STATUS_REJECT_SHORT:
				debug_log_str("<input rejected by short input>\n");
				break;
			case J_RESULT_STATUS_REJECT_LONG:
				debug_log_str("<input rejected by long input>\n");
				break;
			case J_RESULT_STATUS_FAIL:
				debug_log_str("<search failed>\n");
				break;
			}
			/* continue to next process instance */
			continue;
		}

		/* bt->list は時間順に格納されている */
/* bt->list is order by time */
		maxscore = LOG_ZERO;
		tremax = NULL;
		tre = r->backtrellis->list;
		//debug_log_str_int("while start", tre != NULL);
		//debug_log_str_int("am->mfcc->f", r->am->mfcc->f);
		//if(tre != NULL)
			//debug_log_str_int("tre->endtime", tre->endtime);
		while (tre != NULL && tre->endtime + 1 == r->am->mfcc->f) {
			if (maxscore < tre->backscore) {
				maxscore = tre->backscore;
				tremax = tre;
			}
			//debug_log_str_int("maxscore", (int)maxscore);
			tre = tre->next;
		}
		if (maxscore != LOG_ZERO){
			strcat_s(result_string, sizeof(result_string) - 1, "success\t");
			w = tremax->wid;
			//SR instance名
			strcat_s(result_string, sizeof(result_string) - 1, r->config->name);
			strcat_s(result_string, sizeof(result_string) - 1, "\t");
			//文法名
			WORD_ID id_in_all = w;
			WORD_ID left_num = r->lm->winfo->num;
			for (m = r->lm->grammars; m; m = m->next)
			{
				int id = (m->winfo->num + id_in_all) - left_num;
				if (0 <= id)
				{
					//文法名
					strcat_s(result_string, sizeof(result_string) - 1, m->name);
					strcat_s(result_string, sizeof(result_string) - 1, "\t");
					break;
				}
				left_num -= m->winfo->num;
			}
			//認識単語
			strcat_s(result_string, sizeof(result_string), r->lm->winfo->woutput[w]);
			strcat_s(result_string, sizeof(result_string) - 1, "\t");
			//strcat_s(result_string, sizeof(result_string) - 1, "word_id\t");
			//認識単語のID
			strcat_s(result_string, sizeof(result_string) - 1, r->lm->winfo->wname[w]);
			strcat_s(result_string, sizeof(result_string) - 1, "\t");
			//score
			_snprintf_s(temp_buffer, sizeof(temp_buffer), sizeof(temp_buffer), "%5.3f", maxscore);
			strcat_s(result_string, sizeof(result_string) - 1, temp_buffer);
			strcat_s(result_string, sizeof(result_string) - 1, "\t");

			//lm and am score
			strcat_s(result_string, sizeof(result_string) - 1, "\t");
			strcat_s(result_string, sizeof(result_string) - 1, "\n");
		}
		else{
			strcat_s(result_string, sizeof(result_string) - 1, "score is zero\t");
			strcat_s(result_string, sizeof(result_string) - 1, r->config->name);
			strcat_s(result_string, sizeof(result_string) - 1, "\t");
			//文法名
			strcat_s(result_string, sizeof(result_string) - 1, r->lm->grammars->name);
			strcat_s(result_string, sizeof(result_string) - 1, "\n");
		}
		
	}
	output_result(result_string, (int)strlen(result_string));
}


/// <summary>
/// TO-DO: top 3の表示への対応(Unity側のParse処理の変更も必要)
/// </summary>
/// <param name="recog"></param>
/// <param name="dummy"></param>
static void write_result(Recog* recog, void* dummy)
{
	int i, j;
	int len;
	WORD_INFO* winfo;
	WORD_ID* seq;
	int seqnum;
	int n;
	Sentence* s;
	RecogProcess* r;
	HMM_Logical* p;
	SentenceAlign* align;

	char result_string[65535] = "";
	char temp_buffer[65535];
	MULTIGRAM* m;

	/* all recognition results are stored at each recognition process
	instance */
	strcat_s(result_string, sizeof(result_string) - 1, "on_pass2_result\n");
	for (r = recog->process_list; r; r = r->next) {

		/* skip the process if the process is not alive */
		if (!r->live) continue;

		/* result are in r->result.  See recog.h for details */

		/* check result status */
		if (r->result.status < 0) {      /* no results obtained */
										 /* outout message according to the status code */
			strcat_s(result_string, sizeof(result_string) - 1, "error\t");
			//SR instance名
			strcat_s(result_string, sizeof(result_string) - 1, r->config->name);
			strcat_s(result_string, sizeof(result_string) - 1, "\t");
			//文法名
			strcat_s(result_string, sizeof(result_string) - 1, r->lm->grammars->name);
			strcat_s(result_string, sizeof(result_string) - 1, "\n");

			switch (r->result.status) {
			case J_RESULT_STATUS_REJECT_POWER:
				debug_log_str("<input rejected by power>\n");
				break;
			case J_RESULT_STATUS_TERMINATE:
				debug_log_str("<input teminated by request>\n");
				break;
			case J_RESULT_STATUS_ONLY_SILENCE:
				debug_log_str("<input rejected by decoder (silence input result)>\n");
				break;
			case J_RESULT_STATUS_REJECT_GMM:
				debug_log_str("<input rejected by GMM>\n");
				break;
			case J_RESULT_STATUS_REJECT_SHORT:
				debug_log_str("<input rejected by short input>\n");
				break;
			case J_RESULT_STATUS_REJECT_LONG:
				debug_log_str("<input rejected by long input>\n");
				break;
			case J_RESULT_STATUS_FAIL:
				debug_log_str("<search failed>\n");
				break;
			}
			/* continue to next process instance */
			continue;
		}

		/* output results for all the obtained sentences */
		winfo = r->lm->winfo;

		for (n = 0; n < r->result.sentnum; n++) { /* for all sentences */

			s = &(r->result.sent[n]);
			seq = s->word;
			seqnum = s->word_num;

			/* output word sequence like Julius */
			strcat_s(result_string, sizeof(result_string) - 1, "success\t");
			//SR instance名
			strcat_s(result_string, sizeof(result_string) - 1, r->config->name);
			strcat_s(result_string, sizeof(result_string) - 1, "\t");

			for (i = 0; i < seqnum; i++){
				WORD_ID id_in_all = seq[i];
				WORD_ID left_num = r->lm->winfo->num;
				for(m = r->lm->grammars; m; m = m->next)
				{
					int id = (m->winfo->num + id_in_all) - left_num;
					if (0 <= id)
					{
						//文法名
						strcat_s(result_string, sizeof(result_string) - 1, m->name);
						strcat_s(result_string, sizeof(result_string) - 1, "\t");
						break;
					}
					left_num -= m->winfo->num;
				}
				strcat_s(result_string, sizeof(result_string) - 1, winfo->woutput[seq[i]]);
				if (i + 1 == seqnum){
					strcat_s(result_string, sizeof(result_string) - 1, "\t");
				}
				else{
					strcat_s(result_string, sizeof(result_string) - 1, ",");
				}
			}

			/* LM entry sequence */

			//word id
			//strcat_s(result_string, sizeof(result_string) - 1, "word_id\t");
			for (i = 0; i < seqnum; i++){
				strcat_s(result_string, sizeof(result_string) - 1, winfo->wname[seq[i]]);
				if (i + 1 == seqnum){
					strcat_s(result_string, sizeof(result_string) - 1, "\t");
				}
				else{
					strcat_s(result_string, sizeof(result_string) - 1, ",");
				}
			}

			///* confidence scores */
			//strcat_s(result_string, sizeof(result_string) - 1, "confidence_score\t");
			for (i = 0; i < seqnum; i++){
				_snprintf_s(temp_buffer, sizeof(temp_buffer), sizeof(temp_buffer), "%5.3f", s->confidence[i]);
				strcat_s(result_string, sizeof(result_string) - 1, temp_buffer);
				if (i + 1 == seqnum){
					strcat_s(result_string, sizeof(result_string) - 1, "\t");
				}
				else{
					strcat_s(result_string, sizeof(result_string) - 1, ",");
				}
			}

			///* AM and LM scores */
			//strcat_s(result_string, sizeof(result_string) - 1, "total_score\t");
			_snprintf_s(temp_buffer, sizeof(temp_buffer), sizeof(temp_buffer), "%f", s->score);
			strcat_s(result_string, sizeof(result_string) - 1, temp_buffer);
			strcat_s(result_string, sizeof(result_string) - 1, "\t");

			//strcat_s(result_string, sizeof(result_string) - 1, "acoustic_model_score\t");
			_snprintf_s(temp_buffer, sizeof(temp_buffer), sizeof(temp_buffer), "%f", s->score_am);
			strcat_s(result_string, sizeof(result_string) - 1, temp_buffer);
			strcat_s(result_string, sizeof(result_string) - 1, "\t");

			//strcat_s(result_string, sizeof(result_string) - 1, "language_model_score\t");
			_snprintf_s(temp_buffer, sizeof(temp_buffer), sizeof(temp_buffer), "%f", s->score_lm);
			strcat_s(result_string, sizeof(result_string) - 1, temp_buffer);
			strcat_s(result_string, sizeof(result_string) - 1, "\n");
		}
	}

	output_result(result_string, (int)strlen(result_string));
}

/// <summary>
/// 既に登録されている認識プロセスを有効化する。
/// TO-DO:登録されていなければ追加する処理を入れる。
/// 認識処理プロセスの新規追加も必要
/// </summary>
/// <param name="name">process name</param>
/// <returns>0:正常終了,other:異常終了</returns>
UNIJULIUS_API int activate_sr_instance(char* name)
{
	RecogProcess* r;
	if (recog == NULL)
	{
		debug_log_str("You tried to activate process, but UniJulius had not begun");
		return NOT_BEGIN;
	}

	for (r = recog->process_list; r; r = r->next) {
		if (strmatch(r->config->name, name)) {
			/* book to be active at next interval */
			r->active = 1;
			break;
		}
	}
	if (!r) {
		/* not found */
		debug_log_str("You tried to activate process, but that SR instance doesn't exist.");
		return SR_INSTANCE_NOT_FOUND;
	}

	/* tell engine to update */
	recog->process_want_reload = TRUE;
	schedule_grammar_update(recog);

	debug_log_str("That named process was activated");

	return SUCCESS;
}


/// <summary>
/// 認識処理プロセスを無効化する
/// </summary>
/// <param name="name">無効化する認識処理プロセス名</param>
/// <returns>0:正常終了,other:異常終了</returns>
UNIJULIUS_API int deactivate_sr_instance(char* name)
{
	int ret;
	if (recog == NULL){
		debug_log_str("You tried to deactivate process, but UniJulius had not begun");
		return NOT_BEGIN;
	}
	
	RecogProcess* r;

	for (r = recog->process_list; r; r = r->next) {
		if (strmatch(r->config->name, name)) {
			/* book to be inactive at next interval */
			r->active = -1;
			break;
		}
	}
	if (!r) {			/* not found */
		debug_log_str("You tried to deactivate grammar, but that SR instance doesn't exist.");
		return SR_INSTANCE_NOT_FOUND;
	}

	/* tell engine to update */
	recog->process_want_reload = TRUE;
	
	debug_log_str("That named process was deactivated.");
	return SUCCESS;
}


/// <summary>
/// 認識プロセスに文法を読み込む
/// </summary>
/// <param name="winfo"></param>
/// <param name="dfa"></param>
/// <param name="dict_path">辞書ファイルへのパス</param>
/// <param name="dfa_path">文法ファイルへのパス</param>
/// <param name="r">読み込み先の認識処理インスタンス</param>
/// <returns>0:正常終了,-1:異常終了</returns>
static boolean load_grammar(WORD_INFO* winfo, DFA_INFO* dfa, char* dict_path, char* dfa_path, RecogProcess* r)
{
	boolean ret;

	if (!recog) return -1;

	// load grammar
	switch (r->lmvar) {
	case LM_DFA_WORD:
		ret = init_wordlist(winfo, dict_path, r->lm->am->hmminfo,
			r->lm->config->wordrecog_head_silence_model_name,
			r->lm->config->wordrecog_tail_silence_model_name,
			(r->lm->config->wordrecog_silence_context_name[0] == '\0') ? NULL : r->lm->config->wordrecog_silence_context_name,
			r->lm->config->forcedict_flag);
		if (ret == FALSE) {
			return -1;
		}
		break;
	case LM_DFA_GRAMMAR:
		ret = init_voca(winfo, dict_path, r->lm->am->hmminfo, FALSE, r->lm->config->forcedict_flag);
		if (ret == FALSE) {
			return -1;
		}
		ret = init_dfa(dfa, dfa_path);
		if (ret == FALSE) {
			return -1;
		}
		break;
	}

	return 0;
}

/// <summary>
/// 辞書の追加だけをする、音声認識対象には入れない。
/// </summary>
/// <param name="sr_instance_name">追加先の認識処理インスタンス名</param>
/// <param name="grammar_name">追加する辞書（文法）の名前</param>
/// <param name="dict_path">辞書ファイルへのパス</param>
/// <param name="dfa_path">文法ファイルへのパス</param>
/// <returns>0:正常終了,1:既に追加されている, other:異常終了</returns>
UNIJULIUS_API int add_grammar(char* sr_instance_name, char* grammar_name, char* dict_path, char* dfa_path)
{
	WORD_INFO* winfo;
	DFA_INFO* dfa;
	RecogProcess* r;
	boolean ret;
	int gid;
	
	if (!recog) {
		debug_log_str("You tried to add grammar, but UniJulius had not begun");
		return NOT_BEGIN;
	}

	r = recog->process_list;
	//追加する認識処理プロセスを検索
	if (sr_instance_name != NULL && strlen(sr_instance_name) != 0) {
		for (r = recog->process_list; r; r = r->next) {
			if (strmatch(r->config->name, sr_instance_name)) {
				break;
			}
		}
	}

	if (r == NULL) {
		debug_log_str("You tried to add grammar, but that SR instance doesn't exist.");
		return SR_INSTANCE_NOT_FOUND;
	}

	gid = multigram_get_id_by_name(r->lm, grammar_name);
	//既に存在していたらALREADY_DONEを返す
	//追加が失敗したときと区別するために別の値を返した方がいいかもしれない
	if (gid != -1){
		debug_log_str("You tried to add grammar, but the grammar already exist.");
		return ALREADY_DONE;
	}

		// load grammar
	switch (r->lmvar) {
	case LM_DFA_WORD:
		winfo = word_info_new();
		dfa = NULL;
		ret = load_grammar(winfo, NULL, dict_path, NULL, r);
		if (ret != 0) {
			word_info_free(winfo);
			return UNKNOWN_ERROR;
		}
		break;
	case LM_DFA_GRAMMAR:
		winfo = word_info_new();
		dfa = dfa_info_new();
		ret = load_grammar(winfo, dfa, dict_path, dfa_path, r);
		if (ret != 0) {
			word_info_free(winfo);
			dfa_info_free(dfa);
			return UNKNOWN_ERROR;
		}
	}
	//winfo->
	/* register the new grammar to multi-gram tree */
	multigram_add(dfa, winfo, grammar_name, r->lm, NULL);

	gid = multigram_get_id_by_name(r->lm, grammar_name);
	multigram_deactivate(gid, r->lm);
	/* need to rebuild the global lexicon */
	/* tell engine to update at requested timing */
	schedule_grammar_update(recog);
	///* make sure this process will be activated */
	//r->active = 1;
	debug_log_str("New dict was added");
	return SUCCESS;
}

/// <summary>
/// 文法を削除する
/// </summary>
/// <param name="sr_instance_name">削除先の認識処理インスタンス名</param>
/// <param name="grammar_name">削除する辞書（文法）の名前</param>
/// <returns>0:正常終了,other:異常終了</returns>
UNIJULIUS_API int delete_grammar(char* sr_instance_name, char* grammar_name)
{
	RecogProcess* r;
	int gid;

	if (!recog) {
		debug_log_str("You tried to delete grammar, but UniJulius had not begun");
		return NOT_BEGIN;
	}

	r = recog->process_list;
	if (sr_instance_name != NULL && strlen(sr_instance_name) != 0) {
		for (r = recog->process_list; r; r = r->next) {
			if (strmatch(r->config->name, sr_instance_name)) {
				break;
			}
		}
	}

	if (!r){
		debug_log_str("You tried to delete grammar, but that SR instance doesn't exist.");
		return SR_INSTANCE_NOT_FOUND;
	}

	gid = multigram_get_id_by_name(r->lm, grammar_name);
	if (gid == -1){
		debug_log_str("That named grammar was not found");
		return GRAMMAR_NOT_FOUND;
	}

	if (multigram_delete(gid, r->lm) == FALSE) { /* deletion marking failed */
		return GRAMMAR_NOT_FOUND;
	}
	/* need to rebuild the global lexicon */
	/* tell engine to update at requested timing */
	schedule_grammar_update(recog);

	debug_log_str("That named grammar was deleted");

	return SUCCESS;
}

/// <summary>
/// 辞書を音声認識対象に追加。辞書が登録されてなければ登録する。
/// 認識処理インスタンスが無効化されている場合は有効化する。
/// </summary>
/// <param name="sr_instance_name">有効化先の認識処理インスタンス名</param>
/// <param name="grammar_name">有効化する辞書（文法）の名前</param>
/// <param name="dict_path">辞書ファイルへのパス</param>
/// <param name="dfa_path">文法ファイルへのパス</param>
/// <returns>0:正常終了,1:既に有効化されている,other:異常終了</returns>
UNIJULIUS_API int activate_grammar(char* sr_instance_name, char* grammar_name, char* dict_path, char* dfa_path)
{
	RecogProcess* r;
	WORD_INFO* winfo;
	DFA_INFO* dfa;
	int gid;
	int ret;

	if (!recog) {
		debug_log_str("You tried to activate grammar, but UniJulius had not begun");
		return NOT_BEGIN;
	}

	r = recog->process_list;
	if (sr_instance_name != NULL && strlen(sr_instance_name) != 0) {
		for (r = recog->process_list; r; r = r->next) {
			if (strmatch(r->config->name, sr_instance_name)) {
				break;
			}
		}
	}

	if (r == NULL) {
		debug_log_str("You tried to activate grammar, but that SR instance doesn't exist.");
		return SR_INSTANCE_NOT_FOUND;
	}

	gid = multigram_get_id_by_name(r->lm, grammar_name);
	// that name of grammar is not defined
	if (gid == -1) {
		switch (r->lmvar) {
		case LM_DFA_WORD:
			winfo = word_info_new();
			dfa = NULL;
			ret = load_grammar(winfo, NULL, dict_path, NULL, r);
			if (ret != 0) {
				word_info_free(winfo);
				return UNKNOWN_ERROR;
			}
			break;
		case LM_DFA_GRAMMAR:
			winfo = word_info_new();
			dfa = dfa_info_new();
			ret = load_grammar(winfo, dfa, dict_path, dfa_path, r);
			if (ret != 0) {
				word_info_free(winfo);
				dfa_info_free(dfa);
				return UNKNOWN_ERROR;
			}
		}
		/* register the new grammar to multi-gram tree */
		multigram_add(dfa, winfo, grammar_name, r->lm, NULL);
		/* need to rebuild the global lexicon */
		/* tell engine to update at requested timing */
		schedule_grammar_update(recog);
		/* make sure this process will be activated */
		r->active = 1;

		debug_log_str("New dict was added and activated.");
		return SUCCESS;
	}

	ret = multigram_activate(gid, r->lm);
	if (ret == 1) {
		/* already active */
		debug_log_str("That named dict is already active.");
		return ALREADY_DONE;
	}
	else if (ret == -1) {
		/* not found */
		return GRAMMAR_NOT_FOUND;
	}
	/* tell engine to update at requested timing */
	schedule_grammar_update(recog);
	
	debug_log_str("That named dict was activated.");
	return SUCCESS;
}


/// <summary>
/// 文法を音声認識対象から外す
/// </summary>
/// <param name="grammar_name">無効化する辞書（文法）の名前</param>
/// <param name="sr_instance_name">無効化先の認識処理インスタンス名</param>
/// <returns>0:正常終了,1:既に無効化されている,other:異常終了</returns>
UNIJULIUS_API int deactivate_grammar(char* sr_instance_name, char* grammar_name)
{
	RecogProcess* r;
	int gid;
	int ret;

	if (!recog) {
		debug_log_str("You tried to deactivate grammar, but UniJulius had not begun");
		return NOT_BEGIN;
	}

	r = recog->process_list;
	if (sr_instance_name != NULL && strlen(sr_instance_name) != 0) {
		for (r = recog->process_list; r; r = r->next) {
			if (strmatch(r->config->name, sr_instance_name)) {
				break;
			}
		}
	}

	if (!r) {
		debug_log_str("You tried to deactivate grammar, but that SR instance doesn't exist.");
		return SR_INSTANCE_NOT_FOUND;
	}

	gid = multigram_get_id_by_name(r->lm, grammar_name);
	if (gid == -1){
		debug_log_str("That named grammar was not found.");
		return GRAMMAR_NOT_FOUND;
	}

	ret = multigram_deactivate(gid, r->lm);
	if (ret == 1) {
		/* already inactive */
		debug_log_str("That named grammar is already inactive.");
		return ALREADY_DONE;
	}
	else if (ret == -1) {
		/* not found */
		return GRAMMAR_NOT_FOUND;
	}
	/* tell engine to update at requested timing */
	schedule_grammar_update(recog);
	
	debug_log_str("That named grammar was deactivated.");

	return SUCCESS;
}


UNIJULIUS_API int is_UniJulius_active()
{
	if (!recog) {
		debug_log_str("You tried to check if UniJulius is active, but UniJulius had not begun");
		return NOT_BEGIN;
	}
	return recog->process_active;
}


UNIJULIUS_API void set_audio_callback(audio_read_callback_func_type callback)
{
	audio_read_callback = callback;
}

UNIJULIUS_API void set_debug_log_str_func(debug_log_func_str_type debug_log_func)
{
	debug_log_str = debug_log_func;
}

UNIJULIUS_API void set_debug_log_str_int_func(debug_log_func_str_int_type debug_log_func)
{
	debug_log_str_int = debug_log_func;
}

UNIJULIUS_API void set_result_func(result_func_type result_func)
{
	output_result = result_func;
}

UNIJULIUS_API int begin(char* jconf_path)
{
	if (audio_read_callback == NULL)
	{
		debug_log_str("audio callback is null, so use default audio in\n");
	}

	if (output_result == NULL)
	{
		debug_log_str("output_result is null\n");
		return OUTPUT_FUNCTION_IS_NOT_DEFINED;
	}

	clean_up_if_exists();

	jconf = j_config_load_file_new(jconf_path);
	if (jconf == NULL)
	{
		debug_log_str("failed to load jconf\n");
		return FAILED_LOAD_JCONF;
	}

	recog = j_recog_new();
	if (recog == NULL)
	{
		debug_log_str("failed to create recog\n");
		return FAILED_CREATE_RECOG;
	}
	//動的なプロセスの切り替え時にすぐ1st passの出力を得られるようにする
	recog->gram_switch_input_method = SM_PAUSE;

	if (create_engine())
	{
		debug_log_str("successfully engine created");
	}
	else
	{
		debug_log_str("failed to create engine");
		return FAILED_CREATE_ENGINE;
	}
	
	if (j_recognize_stream(recog) < 0){
		debug_log_str("failed to open recognize stream");
		return FAILED_OPEN_STREAM;
	}

	clean_up_if_exists();
	return SUCCESS;
}

UNIJULIUS_API int finish()
{
	if (!recog){
		debug_log_str("You tried to finish UniJulius, but UniJulius had not begun");
		return NOT_BEGIN;
	}
	int ret = j_close_stream(recog);
	if (ret == -2){
		return DEVICE_ERROR;
	}
	else if (ret == -1){
		return UNKNOWN_ERROR;
	}
	else{
		return SUCCESS;
	}
}

UNIJULIUS_API int pause()
{
	if (!recog) {
		debug_log_str("You tried to finish UniJulius, but UniJulius had not begun");
		return NOT_BEGIN;
	}
	j_request_pause(recog);
	return SUCCESS;
}

UNIJULIUS_API int resume()
{
	if (!recog) {
		debug_log_str("You tried to finish UniJulius, but UniJulius had not begun");
		return NOT_BEGIN;
	}
	j_request_resume(recog);
	return SUCCESS;
}


boolean adin_unity_standby(int sfreq, void* dummy)
{
	debug_log_str("mic_standby");
	return TRUE;
}

static boolean adin_unity_open(char* arg)
{
	debug_log_str("mic_open");
	return TRUE;
}

boolean adin_unity_begin(char* arg)
{
	debug_log_str("mic_begin");
	return TRUE;
}

boolean adin_unity_end()
{
	debug_log_str("mic_end");
	return TRUE;
}

int adin_unity_read(SP16* buf, int sampnum)
{
	int len = 0;
	SP16* result = audio_read_callback(sampnum, &len);
	if (len > 0)
	{
		memcpy(buf, result, len * sizeof(SP16));
	}
	return len;
}

boolean adin_unity_pause()
{
	return TRUE;
}

boolean adin_unity_terminate()
{
	return TRUE;
}

boolean adin_unity_resume()
{
	return TRUE;
}

char* adin_unity_input_name()
{
	return "Unity Microphone";
}

bool create_engine()
{
	ADIn* ad_in;

	jconf->input.type = INPUT_WAVEFORM;
	jconf->input.speech_input = SP_MIC;
	jconf->decodeopt.realtime_flag = TRUE;

	recog = j_create_instance_from_jconf(jconf);
	if (recog == NULL)
	{
		debug_log_str("failed to create recog instance");
		return FALSE;
	}

	//Register callbacks
	callback_add(recog, CALLBACK_EVENT_PROCESS_ONLINE, on_process_online, NULL);
	callback_add(recog, CALLBACK_EVENT_PROCESS_OFFLINE, on_process_offline, NULL);
	callback_add(recog, CALLBACK_EVENT_SPEECH_READY, on_speech_ready, NULL);
	callback_add(recog, CALLBACK_EVENT_SPEECH_START, on_speech_start, NULL);
	callback_add(recog, CALLBACK_EVENT_SPEECH_STOP, on_speech_stop, NULL);
	callback_add(recog, CALLBACK_EVENT_RECOGNITION_BEGIN, on_recognition_begin, NULL);
	callback_add(recog, CALLBACK_EVENT_RECOGNITION_END, on_recognition_end, NULL);
	callback_add(recog, CALLBACK_EVENT_PASS1_FRAME, on_pass1_frame, NULL);
	//callback_add(recog, CALLBACK_RESULT_PASS1_INTERIM, on_pass1_frame, NULL);
	callback_add(recog, CALLBACK_EVENT_PAUSE, on_pause, NULL);
	callback_add(recog, CALLBACK_EVENT_RESUME, on_resume, NULL);
	callback_add(recog, CALLBACK_RESULT, on_result, NULL);
	callback_add(recog, CALLBACK_PAUSE_FUNCTION, on_pause_function, NULL);

	if (!j_adin_init(recog))
	{
		debug_log_str("failed to initialize audio in");
		return FALSE;
	}

	if (audio_read_callback != NULL)
	{
		recog->adin->ad_standby = adin_unity_standby;
		recog->adin->ad_begin = adin_unity_begin;
		recog->adin->ad_end = adin_unity_end;
		recog->adin->ad_input_name = adin_unity_input_name;
		recog->adin->ad_read = adin_unity_read;
		recog->adin->ad_pause = adin_unity_pause;
		recog->adin->ad_terminate = adin_unity_terminate;
		recog->adin->ad_resume = adin_unity_resume;
	}

	j_recog_info(recog);

	if (j_open_stream(recog, NULL) < 0)
	{
		debug_log_str("failed to open stream");
		return FALSE;
	}

	return TRUE;
}

