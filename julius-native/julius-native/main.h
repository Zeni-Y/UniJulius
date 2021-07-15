#pragma once

#ifdef JULIUSNATIVE_EXPORTS
#define UNIJULIUS_API __declspec(dllexport)
#else
#define UNIJULIUS_API __declspec(dllimport)
#endif


#include "julius/juliuslib.h"
#include <stdbool.h>

typedef short* (*audio_read_callback_func_type)(int, int*);
typedef void(*debug_log_func_str_type)(const char*);
typedef void(*debug_log_func_str_int_type)(const char*, int);
typedef void(*result_func_type)(const char*, int);

UNIJULIUS_API void set_audio_callback(audio_read_callback_func_type callback);
UNIJULIUS_API void set_debug_log_str_func(debug_log_func_str_type debug_log_func);
UNIJULIUS_API void set_debug_log_str_int_func(debug_log_func_str_int_type debug_log_func);
UNIJULIUS_API void set_result_func(result_func_type result_func);

UNIJULIUS_API int begin(char* jconf_path);
UNIJULIUS_API int finish();
UNIJULIUS_API int pause();
UNIJULIUS_API int resume();
UNIJULIUS_API int is_UniJulius_active();

UNIJULIUS_API int add_grammar(char* sr_instance_name, char* grammar_name, char* dict_path, char* dfa_path);
UNIJULIUS_API int delete_grammar(char* sr_instance_name, char* grammar_name);
UNIJULIUS_API int activate_grammar(char* sr_instance_name, char* grammar_name, char* dict_path, char* dfa_path);
UNIJULIUS_API int deactivate_grammar(char* sr_instance_name, char* grammar_name);

UNIJULIUS_API int activate_sr_instance(char* name);
UNIJULIUS_API int deactivate_sr_instance(char* name);

void clean_up_if_exists();
bool create_engine();

static void write_result(Recog* recog, void* dummy);
