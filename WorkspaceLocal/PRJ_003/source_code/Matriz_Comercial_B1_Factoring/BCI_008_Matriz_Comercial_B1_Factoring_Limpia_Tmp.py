# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_008_Matriz_Comercial_B1_Factoring_Limpia_Tmp

# COMMAND ----------

# MAGIC %md
# MAGIC ## Captura de Variables

# COMMAND ----------

dbutils.widgets.removeAll()
dbutils.widgets.text("platinum_temp_dbW","","01 platinum temp db:")
dbutils.widgets.text("db_location_platinum_tempW","","02 platinum tmp location:")

# COMMAND ----------

platinum_temp_dbX = dbutils.widgets.get("platinum_temp_dbW")
spark.conf.set("bci.platinum_temp_dbX", platinum_temp_dbX)

db_location_platinum_temp_X = dbutils.widgets.get("db_location_platinum_tempW")
spark.conf.set("bci.db_location_platinum_temp_X", db_location_platinum_temp_X)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Funciones

# COMMAND ----------

def sql_safe(query):  
  try:    
    print ("sqlSafe: query -> " + query)    
    return spark.sql(query)  
  except Exception as e:    
    dbutils.notebook.exit("{\"coderror\":\"20001\", \"msgerror\":\"Error grave procesado consulta -> "+str(e)+"\"}")

# COMMAND ----------

# MAGIC %md
# MAGIC ## Eliminacion Tablas Temporales

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tabla Tmp_Fundir_Prov_d00

# COMMAND ----------

paso_tb_del_TmpFundirProvd00 = """DROP TABLE IF EXISTS """ + platinum_temp_dbX + ".Tmp_Fundir_Prov_d00"""

# COMMAND ----------

sql_safe(paso_tb_del_TmpFundirProvd00)

# COMMAND ----------

dbutils.fs.rm(db_location_platinum_temp_X+"/Tmp_Fundir_Prov_d00", True)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tabla Tmp_Prov_Gr_Com_Fact_B1_Result_0719

# COMMAND ----------

paso_tb_del_TmpProvGrComFactB1Re = """DROP TABLE IF EXISTS """ + platinum_temp_dbX + ".Tmp_Prov_Gr_Com_Fact_B1_Result_0719"""

# COMMAND ----------

sql_safe(paso_tb_del_TmpProvGrComFactB1Re)

# COMMAND ----------

dbutils.fs.rm(db_location_platinum_temp_X+"/Tmp_Prov_Gr_Com_Fact_B1_Result_0719", True)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tabla Tmp_erc_prs_cba_hist_rut

# COMMAND ----------

paso_tb_del_Tmpercprscbahistrut = """DROP TABLE IF EXISTS """ + platinum_temp_dbX + ".Tmp_erc_prs_cba_hist_rut"""

# COMMAND ----------

sql_safe(paso_tb_del_Tmpercprscbahistrut)

# COMMAND ----------

dbutils.fs.rm(db_location_platinum_temp_X+"/Tmp_erc_prs_cba_hist_rut", True)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tabla Tmp_prv_rut_estatal

# COMMAND ----------

paso_tb_del_Tmpprvrutestatal = """DROP TABLE IF EXISTS """ + platinum_temp_dbX + ".Tmp_prv_rut_estatal"""

# COMMAND ----------

sql_safe(paso_tb_del_Tmpprvrutestatal)

# COMMAND ----------

dbutils.fs.rm(db_location_platinum_temp_X+"/Tmp_prv_rut_estatal", True)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tabla Tmp_Fundir1_004

# COMMAND ----------

paso_tb_del_TmpFundir1004 = """DROP TABLE IF EXISTS """ + platinum_temp_dbX + ".Tmp_Fundir1_004"""

# COMMAND ----------

sql_safe(paso_tb_del_TmpFundir1004)

# COMMAND ----------

dbutils.fs.rm(db_location_platinum_temp_X+"/Tmp_Fundir1_004", True)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tabla Tmp_CardetIrr

# COMMAND ----------

paso_tb_del_TmpCardetIrr = """DROP TABLE IF EXISTS """ + platinum_temp_dbX + ".Tmp_CardetIrr"""

# COMMAND ----------

sql_safe(paso_tb_del_TmpCardetIrr)

# COMMAND ----------

dbutils.fs.rm(db_location_platinum_temp_X+"/Tmp_CardetIrr", True)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tabla Tmp_Fundir2_004

# COMMAND ----------

paso_tb_del_TmpFundir2004 = """DROP TABLE IF EXISTS """ + platinum_temp_dbX + ".Tmp_Fundir2_004"""

# COMMAND ----------

sql_safe(paso_tb_del_TmpFundir2004)

# COMMAND ----------

dbutils.fs.rm(db_location_platinum_temp_X+"/Tmp_Fundir2_004", True)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tabla Tmp_Pi_Pdi

# COMMAND ----------

paso_tb_del_TmpPiPdi = """DROP TABLE IF EXISTS """ + platinum_temp_dbX + ".Tmp_Pi_Pdi"""

# COMMAND ----------

sql_safe(paso_tb_del_TmpPiPdi)

# COMMAND ----------

dbutils.fs.rm(db_location_platinum_temp_X+"/Tmp_Pi_Pdi", True)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tabla Tmp_Factoring_Nodo

# COMMAND ----------

paso_tb_del_TmpFactoringNodo = """DROP TABLE IF EXISTS """ + platinum_temp_dbX + ".Tmp_Factoring_Nodo"""

# COMMAND ----------

sql_safe(paso_tb_del_TmpFactoringNodo)

# COMMAND ----------

dbutils.fs.rm(db_location_platinum_temp_X+"/Tmp_Factoring_Nodo", True)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tabla tmp_dpf_documento_factoring

# COMMAND ----------

paso_tb_del_tmpdpfdocumentofacto = """DROP TABLE IF EXISTS """ + platinum_temp_dbX + ".tmp_dpf_documento_factoring"""

# COMMAND ----------

sql_safe(paso_tb_del_tmpdpfdocumentofacto)

# COMMAND ----------

dbutils.fs.rm(db_location_platinum_temp_X+"/tmp_dpf_documento_factoring", True)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")