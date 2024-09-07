using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter; 

public static class OnActionOutputter {
	public static async Task OutputEverything(Configuration config, ModFilesystem ck3ModFS, string outputModPath){
		await OutputCustomGameStartOnAction(config);
		if (config.FallenEagleEnabled) {
			await DisableUnneededFallenEagleOnActions(outputModPath);
			await RemoveStruggleStartFromFallenEagleOnActions(ck3ModFS, outputModPath);
		} else { // vanilla
			await RemoveUnneededPartsOfVanillaOnActions(ck3ModFS, outputModPath);
		}
		Logger.IncrementProgress();
	}

	private static async Task RemoveUnneededPartsOfVanillaOnActions(ModFilesystem ck3ModFS, string outputModPath) {
		Logger.Info("Removing unneeded parts of vanilla on-actions...");
		
		// List of blocks to remove as of 2024-09-03.
		Dictionary<string, string[]> partsToRemovePerFile = new() {
			{"yearly_on_actions.txt", [
				"""
							# FP2 - Checks to start El Cid's Travels
							if = {
								limit = { # Am I El Cid?
									this = character:107590
									NOT = { has_character_flag = has_already_begun_travelling } # Separate first check, for performance
				
									NOT = { # Start date employer is either dead or gone
										OR = {
											top_liege = character:107500
											liege = character:107500
											employer = character:107500
										}
									}
									is_available_healthy_ai_adult = yes # Am I ready to go on an adventure?
								}
								trigger_event = fp2_struggle.2045
							}
				""",
			]},
			{"death.txt", [
				"""
						# Fix gods-damned Bavaria splitting from East Francia in an ugly fashion in 867.
						if = {
							limit = {
								# Make sure we're looking at the right guy & that the circumstances haven't changed too much.
								this = character:90107
								highest_held_title_tier = tier_kingdom
								has_realm_law = confederate_partition_succession_law
								# Bavaria should be in a fit state for interfering with the handout order.
								title:k_bavaria = {
									OR = {
										is_title_created = no
										holder = root
									}
									any_in_de_jure_hierarchy = {
										tier = tier_county
										# More than 50%.
										count >= 22
										holder = {
											any_liege_or_above = { this = root }
										}
									}
								}
								NOT = { has_primary_title = title:k_bavaria }
								# Players can sort this themselves: you just need to have Bavaria as your primary title and it's all fine.
								is_ai = yes
							}
							# If we've got no Bavaria, create it.
							if = {
								limit = {
									title:k_bavaria = { is_title_created = no }
								}
								create_title_and_vassal_change = {
									type = created
									save_scope_as = change
								}
								title:k_bavaria = {
									change_title_holder = {
										holder = root
										change = scope:change
									}
								}
								resolve_title_and_vassal_change = scope:change
							}
							# Then switch around.
							set_primary_title_to = title:k_bavaria
						}
				""",
			]},
			{"game_start.txt", [
				// events
				"\t\tfp1_scandinavian_adventurers.0011\t# FP1 - Corral famous Norse adventurers that haven't done much yet.\n",
				"\t\tfp1_scandinavian_adventurers.0021\t# FP1 - Mark game-start prioritised adventurers.\n",
				"\t\teasteregg_event.0001\t\t\t\t# Charna and Jakub duel.\n",
				"\t\tgame_rule.1011\t#Hungarian Migration management.\n",
				"""
						### 867 - RADHANITES IN KHAZARIA ###
						character:74025 = {
							if = {
								limit = {
									is_alive = yes
									is_landed = yes
								}
							}
							trigger_event = bookmark.0200
						}
				""",
				"""
						### 867 - WRATH OF THE NORTHMEN ###
						#Æthelred dying (probably)
						character:33358 = {
							if = {
								limit = {
									is_alive = yes
									is_landed = yes
								}
								trigger_event = {
									id = bookmark.0001
									days = { 365 730 }
								}
							}
						}
				""",
				"""
						#Alfred the Great becoming the Great
						character:7627 = {
							if = {
								limit = {
									is_alive = yes
									is_landed = yes
								}
								trigger_event = {
									id = bookmark.0002
									days = 1800 #~5 years
								}
							}
						}
				""",
				"""
						### 867 - THE GREAT ADVENTURERS ###
						character:251187 = {
							if = {
								limit = {
									is_alive = yes
									is_landed = yes
									AND = {
										character:251180 = { is_ai = yes }
										character:251181 = {
											is_ai = yes
											is_alive = yes
										}
									}
								}
								trigger_event = {
									id = bookmark.0101
									days = { 21 35 }
								}
							}
						}
				""",
				// setup
				"""
						### 1066 - LOUIS THE GERMAN ###
						if = {
							limit = {
								exists = character:90107
								current_date >= 1066.1.1
							}
							character:90107 = { give_nickname = nick_the_german_post_mortem }
						}
				""",
				"""
						# UNITY CONFIG
						## 867.
						if = {
							limit = { game_start_date = 867.1.1 }
							# Twiddle some starting unities.
							## The Abassids are in the middle of a self-killing frenzy, so we lower theirs substantially.
							house:house_abbasid ?= {
								add_unity_value = {
									value = -100
									# This is from historical circumstances, so we just do use the house head.
									character = house_head
									desc = clan_unity_historical_circumstances.desc
								}
							}
							## The Samanids are juuuuust about to get started on killing each other over who gets to lead Transoxiana.
							house:house_samanid ?= {
								add_unity_value = {
									value = -40
									# This is from historical circumstances, so we just do use the house head.
									character = house_head
									desc = clan_unity_historical_circumstances.desc
								}
							}
							## The Afrighids (both of them) are having fairly few arguments because only one of them can speak and it's very easy to manage relations with a baby.
							dynasty:1042112.dynast.house ?= {
								add_unity_value = {
									value = 50
									# This is from historical circumstances, so we just do use the house head.
									character = house_head
									desc = clan_unity_historical_circumstances.desc
								}
							}
							## The Tahirids are scattered but actually get along quite well and support each other politically (mostly).
							dynasty:811.dynast.house ?= {
								add_unity_value = {
									value = 100
									# This is from historical circumstances, so we just do use the house head.
									character = house_head
									desc = clan_unity_historical_circumstances.desc
								}
							}
							## The Umayyads are having something of a renaissance.
							dynasty:597.dynast.house ?= {
								add_unity_value = {
									value = 100
									# This is from historical circumstances, so we just do use the house head.
									character = house_head
									desc = clan_unity_historical_circumstances.desc
								}
							}
						}
						# LEGITIMACY CONFIG
						## 867.
						if = {
							limit = { game_start_date = 867.1.1 }
							## Basileus Basileios was actually elected, so he's technically legitimate, but starts at level 2. With this he should be level 3.
							character:1700 = {
								add_legitimacy = major_legitimacy_gain
							}
						}
				""",
				"""
						if = { # Special historical events for Matilda!
							limit = {
								character:7757 ?= { is_alive = yes }
							}
							character:7757 ?= {
								trigger_event = bookmark.1066 # Matildas marriage to her step-brother, with plausible historical options!
								trigger_event = { # Matildas suspected witchcraft, the player decides if its true or not!
									id = bookmark.1067
									years = { 1 5 }
								}
							}
						}
				""",
				"""
						if = { # Special historical events for Vratislav!
							limit = {
								character:522 ?= { is_alive = yes }
							}
							character:522 ?= {
								trigger_event = { # Vratislav and the Slavic Marches, he didn't historically get them (one briefly, but eh). The player chooses to appease the emperor or go after the coveted lands themselves!
									id = bookmark.1068
									days = { 35 120 }
								}
								trigger_event = { # Jaromir, Vratislav's brother, was a pain - this event is an opportunity for the player to handle the rivalry
									id = bookmark.1069
									days = { 1 29 }
								}
							}
						}
				""",
				"""
						if = { # Special historical events for Robert the Fox!
							limit = {
								character:1128 ?= { is_alive = yes }
							}
							character:1128 ?= {
								trigger_event = { # A Norman Sicily - Robert historically conquered quite a bit here, the player can choose how far they want to go and the risk they want to take. The more risk, the more event troops/claims.
									id = bookmark.1070
									days = { 35 120 }
								}
								trigger_event = { # The Pretender Monk - Raiktor is a historical character, a monk wo pretended to be a deposed Byzantine emperor which Robert used to beat up Byzantium. Here you can follow historical conquests (taking a bit of the coast) or go full on 'install him as emperor for real'-mode!
									id = bookmark.1071
									years = { 1 7 }
								}
							}
						}
				""",
				"""
						if = { # Special historical events for Emir Yahya!
							limit = {
								character:3924 ?= { is_alive = yes }
							}
							character:3924 ?= {
								trigger_event = { # Conquering Cordoba - Gain an opportunity to conquer Cordoba while gaining one of two buffs; one intrigue-focused, and one military. Historically he was poisoned after having conquered the city... but that's no fun for the player!
									id = bookmark.1072
									days = { 10 35 }
								}
							}
						}
				""",
				"""
						# Pre-defined historic regencies setup.
						## NOTE: we do these first to avoid feed messages getting weird due to regents being replaced immediately after getting their position.
						## 867.
						### None. Yet.
						## 1066.
						if = {
							limit = { game_start_date = 1066.9.15 }
							# Designate some regents.
							## King Philippe of France & Duke Boudewijn of Flanders (friend of his dad's)
							character:214 = {
								designate_diarch = character:364
								# Baldwin of Flanders also promised the prior king he'd take care of Philippe, so we add that starting loyalty hook.
								add_hook = {
									type = predecessor_loyalty_hook
									target = character:364
									years = historic_regent_loyal_after_death_hook_duration_years_char_214_value
								}
							}
							### Plus remember who the promise was made to.
							character:364 = {
								add_opinion = {
									target = character:214
									modifier = promise_to_predecessor
									opinion = 50
								}
								set_variable = {
									name = promise_to_predecessor
									value = character:208
									years = historic_regent_loyal_after_death_hook_duration_years_char_214_value
								}
							}
							## Count Bouchard of Vendome & Guy de Bachaumont (his uncle)
							character:40905 = { designate_diarch = character:40376 }
							## Caliph al-Mustansir & Rasad (his mother)
							character:3096 = { designate_diarch = character:additional_fatimids_1 }
							## Count Ermengol of Urgell & Infanta Sancha of Aragon (his stepmother)
							character:110550 = { designate_diarch = character:110514 }
							## Duke Dirk of Holland & Count Robrecht of Zeeland (his stepfather)
							character:106520 = { designate_diarch = character:368 }
							## Duke Sven of Ostergotland & Kol Sverker (his father)
							character:100530 = { designate_diarch = character:100529 }
							## King Salamon of Hungary & Queen Mother Anastasia (his mother, in the absence of any better recorded options, and to keep other hostile relatives out of the job)
							character:476 = { designate_diarch = character:637 }
							## Prince Demetre of Georgia & Alda Oseti (his mother)
							character:9957 = { designate_diarch = character:9956 }
							## Sultan al-Muazzam Alp Arslan and Hassan "the Order of the Realm".
							character:3040 = {
								designate_diarch = character:3050
								# This is a vizierate as well, so start the diarchy manually.
								start_diarchy = vizierate
								# Tell Alp that he appointed Hassan so he remembers not to dismiss him.
								set_variable = {
									name = my_vizier
									value = character:3050
								}
							}
							# Plus remove all the generated opinions.
							## King Philippe of France & Duke Boudewijn of Flanders
							remove_generated_diarch_consequences_effect = {
								NEW_DIARCH = character:364
								LIEGE = character:214
							}
							## Count Bouchard of Vendome & Guy de Bachaumont
							remove_generated_diarch_consequences_effect = {
								NEW_DIARCH = character:40376
								LIEGE = character:40905
							}
							## Caliph al-Mustansir & Rasad
							remove_generated_diarch_consequences_effect = {
								NEW_DIARCH = character:additional_fatimids_1
								LIEGE = character:3096
							}
							## Count Ermengol of Urgell & Infanta Sancha of Aragon
							remove_generated_diarch_consequences_effect = {
								NEW_DIARCH = character:110514
								LIEGE = character:110550
							}
							## Duke Dirk of Holland & Count Robrecht of Zeeland
							remove_generated_diarch_consequences_effect = {
								NEW_DIARCH = character:368
								LIEGE = character:106520
							}
							## Duke Sven of Ostergotland & Kol Sverker
							remove_generated_diarch_consequences_effect = {
								NEW_DIARCH = character:100529
								LIEGE = character:100530
							}
							## King Salamon of Hungary & Queen Mother Anastasia
							remove_generated_diarch_consequences_effect = {
								NEW_DIARCH = character:637
								LIEGE = character:476
							}
							## Prince Demetre of Georgia & Alda Oseti
							remove_generated_diarch_consequences_effect = {
								NEW_DIARCH = character:9956
								LIEGE = character:9957
							}
							## Sultan al-Muazzam Alp Arslan and Hassan "the Order of the Realm".
							remove_generated_diarch_consequences_effect = {
								NEW_DIARCH = character:3050
								LIEGE = character:3040
							}
						}
				""",
				"""
						## Fatimid Caliphate - basically stuck in the back-end of an entrenched regencies from game start.
						if = {
							limit = { exists = character:3096 }
							character:3096 = { trigger_event = diarchy.0012 }
						}
				""",
				"""
				
						### STRUGGLES ###
						if = {
							limit = { current_date = 867.1.1 }
				
							# Iberian Struggle
							if = { # If we're in 867, Aragonese should be removed from the Struggle, since they don't quite exist yet.
								limit = { exists = struggle:iberian_struggle }
								struggle:iberian_struggle = { set_culture_as_uninvolved = culture:aragonese }
							}
				
							# Persian Struggle
							if = { # If the load order ever changes this struggle is going to break. This must always be read before the struggle.
								limit = { exists = struggle:persian_struggle }
								debug_log = "Samarra Struggle: Gamne start data has been set"
								struggle:persian_struggle = { # Use the object explorer to debug this data (yes, the time has come to learn how to use it)
				
									# Struggle on_start
									fp3_remove_vassal_contract_cooldown_for_tension_effect = yes # todo_cd_hci check if this is something we even want in the struggle anymore
									
									# Flag some titles as un-dissolutionable within the struggle.
									title:e_arabia = { set_variable = struggle_block_dissolution_faction }
									title:d_sunni = { set_variable = struggle_block_dissolution_faction }
								}
							}
						}
				""",
				// achievements
				"""
						### ACHIEVEMENT TRACKING FOR STARTING CHARACTERS
						if = { 
							limit = { has_multiple_players = no }
							every_player = {
								# Base Title
								if = {
									limit = { 
										exists = character:7757
										this = character:7757
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_give_a_dog_a_bone_achievement
										VALUE = yes
									}
								}	
								if = {
									limit = { 
										exists = character:1128
										this = character:1128
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_wily_as_the_fox_achievement
										VALUE = yes
									}
								}
								if = {
									limit = {
										OR = {
											AND = {
												exists = character:108501
												this = character:108501
											}
											AND = {
												exists = character:107500
												this = character:107500
											}
											AND = {
												exists = character:107501
												this = character:107501
											}
											AND = {
												exists = character:108500
												this = character:108500
											}
											AND = {
												exists = character:109500
												this = character:109500
											}
										}
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_sibling_rivalry_achievement
										VALUE = yes
									}
								}
								if = {
									limit = {
										OR = {
											AND = {
												exists = character:163108
												this = character:163108
											}
											AND = {
												exists = character:163110
												this = character:163110
											}
											AND = {
												exists = character:163111
												this = character:163111
											}
											AND = {
												exists = character:163112
												this = character:163112
											}
											AND = {
												exists = character:163119
												this = character:163119
											}
										}
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_blood_eagle_achievement
										VALUE = yes
									}
								}
								if = {
									limit = {
										exists = character:6448
										this = character:6448
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_kings_to_the_seventh_generation_achievement
										VALUE = yes
									}
								}
								if = {
									limit = {
										exists = character:140
										this = character:140
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_norman_yoke_achievement
										VALUE = yes
									}
								}
								if = {
									limit = {
										exists = character:522
										this = character:522
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_royal_dignity_achievement
										VALUE = yes
									}
								}
								if = {
									limit = {
										exists = character:40605
										this = character:40605
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_land_of_the_rus_achievement
										VALUE = yes
									}
								}
								if = {
									limit = {
										exists = character:251187
										this = character:251187
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_mother_of_us_all_achievement
										VALUE = yes
									}
								}
								if = {
									limit = {
										OR = {
											culture = { has_cultural_pillar = heritage_iberian }
											culture = culture:andalusian
										}
										has_religion = religion:christianity_religion
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_reconquista_achievement
										VALUE = yes
									}
								}
								if = {
									limit = {
										culture = culture:irish
										capital_province = { geographical_region = custom_ireland }
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_the_emerald_isle_achievement
										VALUE = yes
									}
								}
								if = {
									limit = {
										OR = {
											culture = culture:castilian
											culture = culture:basque
											culture = culture:portuguese
											culture = culture:catalan
											culture = culture:andalusian
											culture = culture:visigothic
											culture = culture:suebi
										}
										has_religion = religion:islam_religion
										capital_province = { geographical_region = world_europe_west_iberia }
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_al_andalus_achievement
										VALUE = yes
									}
								}
								if = {
									limit = { 
										exists = character:159137
										this = character:159137
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_almost_there_achievement
										VALUE = yes
									}
								}
								if = {
									limit = { 
										exists = character:109607
										this = character:109607
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_last_count_first_king_achievement
										VALUE = yes
									}
								}
								if = {
									limit = { 
										exists = character:6878
										this = character:6878
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_going_places_achievement
										VALUE = yes
									}
								}
								# FP1
								## far_from_home_achievement
								if = {
									limit = {
										# Starting as a Norse pagan Norse-cultured character.
										fp1_achievement_culture_plus_religion_norse_trigger = yes
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_far_from_home_achievement
										VALUE = yes
									}
								}
								## miklagardariki_achievement
								if = {
									limit = {
										# Starting as a Norse pagan Norse-cultured character.
										fp1_achievement_culture_plus_religion_norse_trigger = yes
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_miklagardariki_achievement
										VALUE = yes
									}
								}
								## canute_the_greater_achievement
								add_achievement_global_variable_effect = {
									VARIABLE = started_canute_the_greater_achievement
									VALUE = yes
								}
								## king_of_all_the_isles_achievement
								if = {
									limit = {
										# Starting as a Norse pagan Norse-cultured character.
										fp1_achievement_culture_plus_religion_norse_trigger = yes
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_king_of_all_the_isles_achievement
										VALUE = yes
									}
								}
								## faster_than_the_fox_achievement
								if = {
									limit = {
										# Starting as a Norse pagan Norse-cultured character.
										fp1_achievement_culture_plus_religion_norse_trigger = yes
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_faster_than_the_fox_achievement
										VALUE = yes
									}
								}
								## volva_achievement
								if = {
									limit = {
										# Starting as a Norse pagan Norse-cultured character.
										fp1_achievement_culture_plus_religion_norse_trigger = yes
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_volva_achievement
										VALUE = yes
									}
								}
								## saga_in_stone_achievement
								add_achievement_global_variable_effect = {
									VARIABLE = started_saga_in_stone_achievement
									VALUE = yes
								}
								## first_of_the_crusader_kings_achievement
								if = {
									limit = {
										# Starting as a Norse-cultured character.
										fp1_achievement_culture_norse_trigger = yes
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_first_of_the_crusader_kings_achievement
										VALUE = yes
									}
								}
								## vladimirs_second_choice_achievement
								if = {
									limit = {
										# Starting as a Norse pagan Norse or Rus-cultured character.
										fp1_achievement_culture_norse_or_rus_trigger = yes
										fp1_achievement_religious_norse_trigger = yes
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_vladimirs_second_choice_achievement
										VALUE = yes
									}
								}
								## a_dangerous_business_achievement
								add_achievement_global_variable_effect = {
									VARIABLE = started_a_dangerous_business_achievement
									VALUE = yes
								}
								# EP1
								##1 Patronage
								add_achievement_global_variable_effect = {
									VARIABLE = started_patronage_achievement
									VALUE = yes
								}
								##2 Converging Paths
								add_achievement_global_variable_effect = {
									VARIABLE = started_converging_paths_achievement
									VALUE = yes
								}
								##3 Changing course
								add_achievement_global_variable_effect = {
									VARIABLE = started_changing_course_achievement
									VALUE = yes
								}
								##4 Hoarder
								add_achievement_global_variable_effect = {
									VARIABLE = started_hoarder_achievement
									VALUE = yes
								}
								##5 creme de la creme
								add_achievement_global_variable_effect = {
									VARIABLE = started_creme_de_la_creme_achievement
									VALUE = yes
								}
								##6 Give it back!
								add_achievement_global_variable_effect = {
									VARIABLE = started_polyglot_achievement
									VALUE = yes
								}
								##7 Inspirational
								add_achievement_global_variable_effect = {
									VARIABLE = started_inspirational_achievement
									VALUE = yes
								}
								##8 One of a Kind
								add_achievement_global_variable_effect = {
									VARIABLE = started_one_of_a_kind_achievement
										VALUE = yes
								}
								##9 True Tolerance
								add_achievement_global_variable_effect = {
									VARIABLE = started_true_tolerance_achievement
									VALUE = yes
								}
								##10 Delusions of Grandeur
								add_achievement_global_variable_effect = {
									VARIABLE = started_delusions_of_grandeur_achievement_achievement
									VALUE = yes
								}
								##11 Bod Chen Po
								if = {
									limit = {
										this.dynasty = dynasty:105800
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_bod_chen_po_achievement
										VALUE = yes
									}
								}
								##12 Turkish Eagle
								if = {
									limit = {
										has_title = title:c_samosata
										this.house = house:house_seljuk # Seljuk
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_turkish_eagle_achievement
										VALUE = yes
									}
								}
								##13 Rise of the Ghurids
								if = {
									limit = {
										has_title = title:d_ghur
										this.dynasty = dynasty:791 #Ghurid
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_rise_of_the_ghurids_achievement
										VALUE = yes
									}
								}
								##14 Brave and Bold
								if = {
									limit = {
										game_start_date < 868.1.1
										this.dynasty = dynasty:699 #Piast
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_brave_and_bold_achievement
										VALUE = yes
									}
								}
								##15 Lingua Franca
								add_achievement_global_variable_effect = {
									VARIABLE = started_lingua_franca_achievement
									VALUE = yes
								}
								##16 Beta Israel
								add_achievement_global_variable_effect = {
									VARIABLE = started_beta_israel_achievement
									VALUE = yes
								}
								## 17 They belong in a museum!
								add_achievement_global_variable_effect = {
									VARIABLE = started_they_belong_in_a_museum_achievement
									VALUE = yes
								}
								##18 I made this!
								add_achievement_global_variable_effect = {
									VARIABLE = started_i_made_this_achievement
									VALUE = yes
								}
								##19 Nobody Comes to Fika!
								add_achievement_global_variable_effect = {
									VARIABLE = started_nobody_comes_to_fika_achievement
									VALUE = yes
								}
								## 20 The True Royal Court
								add_achievement_global_variable_effect = {
									VARIABLE = started_the_true_royal_court_achievement
										VALUE = yes
								}
								# EP2
								## 01. The Grandest Tour
								add_achievement_global_variable_effect = {
									VARIABLE = started_the_grandest_tour_achievement
									VALUE = yes
								}
								## 02. Your Eternal Reward
								add_achievement_global_variable_effect = {
									VARIABLE = started_your_eternal_reward_achievement
									VALUE = yes
								}
								## 03. Imperial March
								add_achievement_global_variable_effect = {
									VARIABLE = started_imperial_march_achievement
									VALUE = yes
								}
								## 04. Black Dinner
								add_achievement_global_variable_effect = {
									VARIABLE = started_black_dinner_achievement
									VALUE = yes
								}
								## 05. There and Back Again
								add_achievement_global_variable_effect = {
									VARIABLE = started_there_and_back_again_achievement
									VALUE = yes
								}
								## 06. The Very Best
								add_achievement_global_variable_effect = {
									VARIABLE = started_the_very_best_achievement
									VALUE = yes
								}
								## 07. Like No One Ever Was
								add_achievement_global_variable_effect = {
									VARIABLE = started_like_no_one_ever_was_achievement
									VALUE = yes
								}
								## 08. A Thousand and One Night
								add_achievement_global_variable_effect = {
									VARIABLE = started_a_thousand_and_one_nights_achievement
									VALUE = yes
								}
								## 09. A Knight's Tale
								add_achievement_global_variable_effect = {
									VARIABLE = started_a_knights_tale_achievement
									VALUE = yes
								}
								## 10. Hunting Accident
								add_achievement_global_variable_effect = {
									VARIABLE = started_hunting_accident_achievement
									VALUE = yes
								}
								## 11. Lions and Tigers and Bears, Oh My!
								add_achievement_global_variable_effect = {
									VARIABLE = started_lions_and_tigers_and_bears_oh_my_achievement
									VALUE = yes
								}
								## 12. Fly, my Pretty!
								add_achievement_global_variable_effect = {
									VARIABLE = started_fly_my_pretty_achievement
									VALUE = yes
								}
								## 13. Pathway to Heaven
								add_achievement_global_variable_effect = {
									VARIABLE = started_pathway_to_heaven_achievement
									VALUE = yes
								}
								## 14. Sir Lance-a-Lot
								add_achievement_global_variable_effect = {
									VARIABLE = started_sir_lance_a_lot_achievement
									VALUE = yes
								}
								## 15. I'm in my Element(s)
								add_achievement_global_variable_effect = {
									VARIABLE = started_im_in_my_elements_achievement
									VALUE = yes
								}
								## 16. Ahab
								add_achievement_global_variable_effect = {
									VARIABLE = started_ahab_achievement
									VALUE = yes
								}
								## 17. Little William Marshal
								add_achievement_global_variable_effect = {
									VARIABLE = started_little_william_marshal_achievement
									VALUE = 0
								}
								add_achievement_global_variable_effect = {
									VARIABLE = little_william_marshal_achievement_tally
									VALUE = 0
								}
								## 18. A True & Perfect Knight
								add_achievement_global_variable_effect = {
									VARIABLE = started_a_true_and_perfect_knight_achievement
									VALUE = yes
								}
								## 19. A.E.I.O.U & Me
								if = {
									limit = {
										# Etichonen, of whom the Hapsburgs are a cadet - we check dynasty rather than house so that an accidental cadet doesn't screw you.
										this.house ?= house:house_habsburg
									}
									add_achievement_global_variable_effect = {
										VARIABLE = started_a_e_i_o_u_and_me_achievement
										VALUE = yes
									}
								}
								## 20. The Iron and Golden King
								add_achievement_global_variable_effect = {
									VARIABLE = started_the_iron_and_golden_king_achievement
									VALUE = yes
								}
				
								### RULER DESIGNER ACHIEVEMENT BLOCKS ###
								if = {
									limit = {
										num_virtuous_traits >= 3
									}
									add_achievement_flag_effect = { FLAG = rd_character_blocked_paragon_of_virtue_achievement_flag	}	
								}
								if = {
									limit = {
										any_child = {
											count >= 10
											is_alive = yes
										}
									}
									add_achievement_flag_effect = { FLAG = rd_character_blocked_the_succession_is_safe_achievement_flag }	
								}
								if = {
									limit = {
										any_child = {
											has_trait = inbred
										}	
									}
									add_achievement_flag_effect = { FLAG = rd_character_blocked_keeping_it_in_the_family_achievement_flag }
								}
								if = {
									limit = {
										highest_held_title_tier >= tier_empire
										should_be_naked_trigger = yes	
									}
									add_achievement_flag_effect = { FLAG = rd_character_blocked_the_emperors_new_clothes_achievement_flag }
								}
								if = {
									limit = {
										is_from_ruler_designer = yes
										OR = {
											fp1_achievement_culture_norse_trigger = yes
											fp1_achievement_religious_norse_trigger = yes
										}
									}
									add_to_global_unavailable_achievements_list_effect = { FLAG = flag:rd_character_blocked_far_from_home_achievement }
									add_to_global_unavailable_achievements_list_effect = { FLAG = flag:rd_character_blocked_miklagardariki_achievement }
									add_to_global_unavailable_achievements_list_effect = { FLAG = flag:rd_character_blocked_faster_than_the_fox_achievement }
								}
								if = {
									limit = {
										any_ruler = {
											is_from_ruler_designer = yes
										}
									}
									add_to_global_unavailable_achievements_list_effect = { FLAG = flag:rd_character_blocked_iberia_or_iberia_achievement }
									add_to_global_unavailable_achievements_list_effect = { FLAG = flag:rd_character_blocked_el_cid_achievement }
									add_achievement_global_variable_effect = {
										VARIABLE = any_ruler_designed_character_achievement
										VALUE = yes
									}
								}
							}
						}
				""",
				"""
						### ACHIEVEMENT (FP3): The Ummayad Strikes Back
						every_player = {
							if = {
								limit = {
									dynasty = character:73683.dynasty
									location = { geographical_region = world_europe_west_iberia }
								}
								set_global_variable = fp3_the_umma_strikes_back_achievement_tracker # Is not removed (sad!)
							}
						}
				""",
				]
			},
		};

		foreach (var (file, partsToRemove) in partsToRemovePerFile) {
			var relativePath = Path.Join("common/on_action", file);
			var inputPath = ck3ModFS.GetActualFileLocation(relativePath);
			if (!File.Exists(inputPath)) {
				Logger.Debug($"{relativePath} not found.");
				return;
			}

			string lineEndings = GetLineEndingsInFile(inputPath);
			
			var fileContent = await File.ReadAllTextAsync(inputPath);

			foreach (var block in partsToRemove) {
				// If the file uses other line endings than CRLF, we need to modify the search string.
				if (lineEndings == "LF") {
					fileContent = fileContent.Replace(block.Replace("\r\n", "\n"), "");
				} else if (lineEndings == "CR") {
					fileContent = fileContent.Replace(block.Replace("\r\n", "\r"), "");
				} else {
					fileContent = fileContent.Replace(block, "");
				}
			}

			var outputPath = $"{outputModPath}/{relativePath}";
			await using var output = FileHelper.OpenWriteWithRetries(outputPath);
			await output.WriteAsync(fileContent);
		}
	}
	
	private static string GetLineEndingsInFile(string filePath) {
		using StreamReader sr = new StreamReader(filePath);
		bool returnSeen = false;
		while (sr.Peek() >= 0) {
			char c = (char)sr.Read();
			if (c == '\n') {
				return returnSeen ? "CRLF" : "LF";
			}
			else if (returnSeen) {
				return "CR";
			}

			returnSeen = c == '\r';
		}

		if (returnSeen) {
			return "CR";
		} else {
			return "LF";
		}
	}

	public static async Task OutputCustomGameStartOnAction(Configuration config) {
		Logger.Info("Writing game start on-action...");

		var sb = new StringBuilder();
		
		const string customOnGameStartOnAction = "irtock3_on_game_start_after_lobby";
		
		sb.AppendLine("on_game_start_after_lobby = {");
		sb.AppendLine($"\ton_actions = {{ {customOnGameStartOnAction } }}");
		sb.AppendLine("}");
		
		sb.AppendLine($"{customOnGameStartOnAction} = {{");
		sb.AppendLine("\teffect = {");
		
		if (config.LegionConversion == LegionConversion.MenAtArms) {
			sb.AppendLine("""
			                            	# IRToCK3: add MAA regiments
			                            	random_player = {
			                            		trigger_event = irtock3_hidden_events.0001
			                            	}
			                            """);
		}

		if (config.LegionConversion == LegionConversion.MenAtArms) {
			sb.AppendLine("\t\tset_global_variable = IRToCK3_create_maa_flag");
        }

		if (config.FallenEagleEnabled) {
			// As of the "Last of the Romans" update, TFE only disables Nicene for start dates >= 476.9.4.
			// But for the converter it's important that Nicene is disabled for all start dates >= 451.8.25.
			sb.AppendLine("""
			                            	# IRToCK3: disable Nicene after the Council of Chalcedon.
			                            	if = {
			                            		limit = {
			                            			game_start_date >= 451.8.25
			                            		}
			                            		faith:armenian_apostolic = {
			                            			remove_doctrine = unavailable_doctrine
			                            		}
			                            		faith:nestorian = {
			                            			remove_doctrine = unavailable_doctrine
			                            		}
			                            		faith:coptic = {
			                            			remove_doctrine = unavailable_doctrine
			                            		}
			                            		faith:syriac = {
			                            			remove_doctrine = unavailable_doctrine
			                            		}
			                            		faith:chalcedonian = {
			                            			remove_doctrine = unavailable_doctrine
			                            		}
			                            		faith:nicene = {
			                            			add_doctrine = unavailable_doctrine
			                            		}
			                            	}
			                            """);
		}
		
		sb.AppendLine("\t}");
		sb.AppendLine("}");
		
		var filePath = $"output/{config.OutputModName}/common/on_action/IRToCK3_game_start.txt";
		await using var writer = new StreamWriter(filePath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
		await writer.WriteAsync(sb.ToString());
	}

	private static async Task DisableUnneededFallenEagleOnActions(string outputModPath) {
		Logger.Info("Disabling unneeded Fallen Eagle on-actions...");
		var onActionsToDisable = new OrderedSet<string> {
			"sea_minority_game_start.txt", 
			"sevenhouses_on_actions.txt", 
			"government_change_on_actions.txt",
			"tribs_on_action.txt",
			"AI_war_on_actions.txt",
			"senate_tasks_on_actions.txt",
			"new_electives_on_action.txt",
			"tfe_struggle_on_actions.txt",
			"roman_vicar_positions_on_actions.txt",
		};
		foreach (var filename in onActionsToDisable) {
			var filePath = $"{outputModPath}/common/on_action/{filename}";
			await using var writer = new StreamWriter(filePath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
			await writer.WriteLineAsync("# disabled by IRToCK3");
		}
	}

	private static async Task RemoveStruggleStartFromFallenEagleOnActions(ModFilesystem ck3ModFS, string outputModPath) {
		Logger.Info("Removing struggle start from Fallen Eagle on-actions...");
		var inputPath = ck3ModFS.GetActualFileLocation("common/on_action/TFE_game_start.txt");
		if (!File.Exists(inputPath)) {
			Logger.Debug("TFE_game_start.txt not found.");
			return;
		}
		var fileContent = await File.ReadAllTextAsync(inputPath);

		// List of blocks to remove as of 2024-01-07.
		string[] struggleStartBlocksToRemove = [
			"""
					if = {
						limit = {
							AND = {
								game_start_date >= 476.9.4
								game_start_date <= 768.1.1
							}
						}
						start_struggle = { struggle_type = britannia_struggle start_phase = struggle_britannia_phase_migration }
					}
			""",
			"""
					if = {
						limit = {
							game_start_date >= 476.9.4
						}
						start_struggle = { struggle_type = italian_struggle start_phase = struggle_TFE_italian_phase_turmoil }
					}
			""",
			"""
					if = {
						limit = {
							AND = {
								game_start_date <= 651.1.1 # Death of Yazdegerd III
							}
						}
						start_struggle = { struggle_type = roman_persian_struggle start_phase = struggle_TFE_roman_persian_phase_contention }
					}
					start_struggle = { struggle_type = eastern_iranian_struggle start_phase = struggle_TFE_eastern_iranian_phase_expansion }
					start_struggle = { struggle_type = north_indian_struggle start_phase = struggle_TFE_north_indian_phase_invasion }
			""",
		];

		foreach (var block in struggleStartBlocksToRemove) {
			fileContent = fileContent.Replace(block, "");
		}

		var outputPath = $"{outputModPath}/common/on_action/TFE_game_start.txt";
		await using var output = FileHelper.OpenWriteWithRetries(outputPath);
		await output.WriteAsync(fileContent);
	}
}
