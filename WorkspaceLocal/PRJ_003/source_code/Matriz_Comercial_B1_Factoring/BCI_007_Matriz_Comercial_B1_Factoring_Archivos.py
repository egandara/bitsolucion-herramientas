# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_007_Matriz_Comercial_B1_Factoring_Archivos

# COMMAND ----------

# MAGIC %md
# MAGIC ## Información del Notebook

# COMMAND ----------

# MAGIC %md
# MAGIC ### Encabezado
# MAGIC **************************************************************************
# MAGIC * Nombre: 
# MAGIC * Ruta: 
# MAGIC * Autor: Esteban Gándara
# MAGIC * Ing. Rafael Montecinos - rafael.montecinost@bci.cl
# MAGIC * Fecha: 19/05/2025
# MAGIC * Descripción: 
# MAGIC * Documentación: 
# MAGIC ***************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Mantenciones
# MAGIC **************************************************************************
# MAGIC #### Mantención Nro: 
# MAGIC * Autor: <Nombre Autor> (<Empresa del Autor (Bci/Otra)>) - Ing. SW BCI: <Nombre Ing. SW BCI>
# MAGIC * Fecha: <dd/mm/yyyy> 
# MAGIC * Descripción: <Descripción de la mantención>      
# MAGIC ************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tablas Entrada y Salida
# MAGIC **************************************************************************
# MAGIC #### Tablas Entrada: 
# MAGIC * 
# MAGIC * 
# MAGIC ***************************************************************************
# MAGIC #### Tablas Salida: 
# MAGIC * 

# COMMAND ----------

# MAGIC %md
# MAGIC ## Captura de Variables

# COMMAND ----------

dbutils.widgets.removeAll()
dbutils.widgets.text("fechaProcesoW","","01 Fecha Proceso :")
dbutils.widgets.text("platinum_temp_dbW","","02 platinum temp db:")
dbutils.widgets.text("platinum_dbW","","03 platinum db:")
dbutils.widgets.text("plt_output_locW","","04 Ruta out Archivos:")
dbutils.widgets.text("carpeta_archivosW","","05 Carpeta Archivos:")

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignar Objeto a Lectura de Widgets y Variables

# COMMAND ----------

fechaProcesoX = dbutils.widgets.get("fechaProcesoW")
spark.conf.set("bci.fechaProcesoX", fechaProcesoX)

platinum_temp_dbX = dbutils.widgets.get("platinum_temp_dbW")
spark.conf.set("bci.platinum_temp_dbX", platinum_temp_dbX)

platinum_dbX = dbutils.widgets.get("platinum_dbW")
spark.conf.set("bci.platinum_dbX", platinum_dbX)

plt_output_locX = dbutils.widgets.get("plt_output_locW")
spark.conf.set("bci.plt_output_locX", plt_output_locX)

carpeta_archivosX = dbutils.widgets.get("carpeta_archivosW")
spark.conf.set("bci.carpeta_archivosX", carpeta_archivosX)

print("*****Parámetros*****")
print("fechaProcesoX: " + fechaProcesoX)
print("platinum_temp_dbX: " + platinum_temp_dbX)
print("platinum_dbX: " + platinum_dbX)
print("plt_output_locX: " + plt_output_locX)
print("carpeta_archivosX: " + carpeta_archivosX)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Librerías

# COMMAND ----------

import json
from datetime import datetime, date, time, timedelta
from dateutil.relativedelta import relativedelta

# Obtener fecha actual
current_date_str = datetime.now().strftime("%Y-%m-%d")
current_date = datetime.strptime(current_date_str, "%Y-%m-%d")
formatted_date = str(current_date.strftime("%Y%m%d"))

# COMMAND ----------

# MAGIC %md
# MAGIC ###Asigan Variables de fecha

# COMMAND ----------

ano = str(fechaProcesoX)[:4]
mes = str(fechaProcesoX)[4:][:2]
dia = str(fechaProcesoX)[6:][:2]
fechanormativo = str(ano+'-'+mes+'-'+dia)
fechacinta = str(dia+'-'+mes+'-'+ano)
anomes = str(ano+mes)
anomesdia = str(ano+mes+dia)
anomesdia2 = str(ano+'/'+mes+'/'+dia)


print("fecha_Formato1: " + ano)
print("fecha_Formato2: " + mes)
print("fecha_Formato3: " + dia)
print("fecha_Formato4: " + fechanormativo)
print("fecha_Formato5: " + fechacinta)
print("fecha_Formato6: " + anomes)
print("fecha_Formato7: " + anomesdia)
print("fecha_formato8: " + anomesdia2)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Funciones

# COMMAND ----------

# MAGIC %run "./Funciones"

# COMMAND ----------

# MAGIC %md
# MAGIC ## Validaciones

# COMMAND ----------

valida_param_vacio(fechaProcesoX,'fechaProcesoX')
valida_param_vacio(platinum_temp_dbX,'platinum_temp_dbX')
valida_param_vacio(platinum_dbX,'platinum_dbX')

valida_param_vacio(plt_output_locX,'plt_output_locX')
valida_param_vacio(carpeta_archivosX,'carpeta_archivosX')

# COMMAND ----------

valida_bd(platinum_temp_dbX, 'platinum_temp_dbX')
valida_bd(platinum_dbX, 'platinum_dbX')

# COMMAND ----------

valida_fecha_valida(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Fecha vacía

# COMMAND ----------

fecha_vacia(fechaProcesoX)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Fecha futura

# COMMAND ----------

fecha_futura(fechaProcesoX)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Inicio de Lógica

# COMMAND ----------

# MAGIC %md
# MAGIC ### Creación de archivos
# MAGIC
# MAGIC **************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC #### Archivo: PROV_GR_COM_FACT_B1_0719_20250731.csv

# COMMAND ----------

# MAGIC %md
# MAGIC ##### === Query ===

# COMMAND ----------

df = spark.sql("""
select
a.fec_proceso
,a.rut_cliente
,a.dv_cliente
,a.des_razon_social_cliente
,a.rut_deudor
,a.dv_deudor
,a.des_razon_social_deudor
,a.ind_tipo_documento
,a.num_documento
,a.num_cuota
,a.mnt_saldo_documento
,a.ind_codigo_cobranza
,a.ind_actividad_economica
,a.ind_responsabilidad_operacion
,a.fec_vcto_doc_con_prorroga
,a.des_tipo_segmento
,a.des_cartera_del_documento
,a.cod_clasif_cliente
,a.cod_clasif_deudor
,a.num_dias_mora
,a.num_operacion
,a.num_id_documento
,a.ind_fin_de_mes_doc
,a.pct_pi
,a.pct_pdi
,a.mto_pe
from """ + platinum_dbX + """.prv_fac_ft_b1_com a
WHERE fec_proceso = to_date('""" + fechanormativo + """', 'yyyy-MM-dd') 
""")

# COMMAND ----------

# MAGIC %md
# MAGIC ##### === Librerías ===

# COMMAND ----------

from pyspark.sql import functions as F
from datetime import datetime
import re

# COMMAND ----------

# MAGIC %md
# MAGIC ##### === Parámetros de salida ===

# COMMAND ----------

final_name = f"PROV_GR_COM_FACT_B1_0719_"+fechaProcesoX+".csv"
tmp_dir = f"{plt_output_locX}_tmp_text_{int(datetime.now().timestamp())}"
final_path = f"{plt_output_locX}{final_name}"

# COMMAND ----------

# MAGIC %md
# MAGIC ##### === Formateo ===

# COMMAND ----------

# === Helpers de formato ===
def pad_rut(colname):
    # 8 dígitos con ceros + padding a derecha hasta 12 con espacios
    return F.rpad(F.lpad(F.col(colname).cast("string"), 8, '0'), 12, ' ')

def pad_fixed(colname, width):
    return F.rpad(F.col(colname).cast("string"), width, ' ')

def fmt_3dec(colname):
    return F.format_string("%.3f", F.col(colname).cast("double"))

# Normalización robusta de la fecha de vencimiento a DD-MM-YYYY
# 1) extrae sólo dígitos (remueve separadores y hora)
# 2) si parece YYYYMMDD (año 1900..2100) => DD-MM-YYYY
# 3) si parece DDMMYYYY => DD-MM-YYYY
vcto_raw = F.coalesce(F.col("fec_vcto_doc_con_prorroga").cast("string"), F.lit(""))
vcto_digits = F.regexp_replace(vcto_raw, r"[^0-9]", "")

vcto_out = F.when(
    F.length(vcto_digits) == 8,
    F.when(
        (F.substring(vcto_digits, 1, 4) >= F.lit("1900")) & (F.substring(vcto_digits, 1, 4) <= F.lit("2100")),
        # YYYYMMDD -> DD-MM-YYYY
        F.concat_ws("-", F.substring(vcto_digits, 7, 2), F.substring(vcto_digits, 5, 2), F.substring(vcto_digits, 1, 4))
    ).otherwise(
        # DDMMYYYY -> DD-MM-YYYY
        F.concat_ws("-", F.substring(vcto_digits, 1, 2), F.substring(vcto_digits, 3, 2), F.substring(vcto_digits, 5, 4))
    )
).otherwise(
    # Último intento: si Spark puede parsear como date/timestamp
    F.when(F.to_date(F.col("fec_vcto_doc_con_prorroga")).isNotNull(),
           F.date_format(F.to_date(F.col("fec_vcto_doc_con_prorroga")), "dd-MM-yyyy")) \
     .when(F.to_timestamp(F.col("fec_vcto_doc_con_prorroga")).isNotNull(),
           F.date_format(F.to_timestamp(F.col("fec_vcto_doc_con_prorroga")), "dd-MM-yyyy")) \
     .otherwise(F.lit(""))
)

# COMMAND ----------

# MAGIC %md
# MAGIC ##### === Selección con formato exacto en el orden requerido ===

# COMMAND ----------

out = df.select(
    F.date_format("fec_proceso", "yyyy/MM/dd").alias("fec_proceso"),
    pad_rut("rut_cliente").alias("rut_cliente"),
    F.col("dv_cliente").cast("string").alias("dv_cliente"),
    F.upper(F.col("des_razon_social_cliente")).alias("des_razon_social_cliente"),
    pad_rut("rut_deudor").alias("rut_deudor"),
    F.col("dv_deudor").cast("string").alias("dv_deudor"),
    F.upper(F.col("des_razon_social_deudor")).alias("des_razon_social_deudor"),
    F.col("ind_tipo_documento").cast("string").alias("ind_tipo_documento"),
    F.col("num_documento").cast("string").alias("num_documento"),
    F.col("num_cuota").cast("string").alias("num_cuota"),
    F.col("mnt_saldo_documento").cast("string").alias("mnt_saldo_documento"),
    pad_fixed("ind_codigo_cobranza", 4).alias("ind_codigo_cobranza"),  # 0 + espacios
    F.col("ind_actividad_economica").cast("string").alias("ind_actividad_economica"),
    F.col("ind_responsabilidad_operacion").cast("string").alias("ind_responsabilidad_operacion"),
    vcto_out.alias("fec_vcto_doc_con_prorroga"),
    F.upper(F.col("des_tipo_segmento")).alias("des_tipo_segmento"),
    F.upper(F.col("des_cartera_del_documento")).alias("des_cartera_del_documento"),
    F.when(F.trim(F.col("cod_clasif_cliente")) == "", F.lit(None)).otherwise(F.col("cod_clasif_cliente")).cast("string").alias("cod_clasif_cliente"),
    F.when(F.trim(F.col("cod_clasif_deudor")) == "", F.lit(None)).otherwise(F.col("cod_clasif_deudor")).cast("string").alias("cod_clasif_deudor"),
    F.col("num_dias_mora").cast("string").alias("num_dias_mora"),
    F.col("num_operacion").cast("string").alias("num_operacion"),
    F.col("num_id_documento").cast("string").alias("num_id_documento"),
    F.col("ind_fin_de_mes_doc").cast("string").alias("ind_fin_de_mes_doc"),
    fmt_3dec("pct_pi").alias("pct_pi"),
    fmt_3dec("pct_pdi").alias("pct_pdi"),
    F.col("mto_pe").cast("string").alias("mto_pe")
)

cols = [c for c in out.columns]

rdd_lines = out.rdd.map(
    lambda row: ",".join("" if (row[i] is None) else str(row[i]) for i in range(len(cols))) + "\r\n"
)

# COMMAND ----------

# MAGIC %md
# MAGIC ##### === Limpiar destino final si existe ===

# COMMAND ----------

try:
    dbutils.fs.rm(final_path, recurse=False)
except Exception:
    pass

# COMMAND ----------

# MAGIC %md
# MAGIC ##### === Guardar como texto (un archivo) ===

# COMMAND ----------

rdd_lines.coalesce(1).saveAsTextFile(tmp_dir)

# COMMAND ----------

# MAGIC %md
# MAGIC ##### === Renombrar part-00000 a .csv ===

# COMMAND ----------

part_file = [f.path for f in dbutils.fs.ls(tmp_dir) if re.search(r"/part-.*$", f.path)]
if not part_file:
    raise RuntimeError("No se encontró el part-* en el temporal.")
dbutils.fs.mv(part_file[0], final_path)
dbutils.fs.rm(tmp_dir, recurse=True)

print(f"CSV generado en: {final_path}")

# COMMAND ----------

# MAGIC %md
# MAGIC #### Archivo: PROV_GR_COM_FACT_B1_RESULT_0719_20250731.TXT

# COMMAND ----------

# MAGIC %md
# MAGIC ##### === Librerías ===

# COMMAND ----------

from pyspark.sql import functions as F
from datetime import datetime
import time, uuid, re

# COMMAND ----------

# MAGIC %md
# MAGIC ##### === Parámetros de salida ===

# COMMAND ----------

# ========= Parámetros de salida =========
dest_dir = plt_output_locX+carpeta_archivosX
final_name = f"PROV_GR_COM_FACT_B1_RESULT_0719_"+fechaProcesoX+".txt"
final_path = f"{plt_output_locX}{carpeta_archivosX}/{final_name}"
tmp_dir    = f"{plt_output_locX}_tmp_text_{int(datetime.now().timestamp())}"
DELIM      = "\t"
PAD_DOC = False  # <- IMPORTANTE: False = SIN padding en doc_num_documento_fact

# COMMAND ----------

# MAGIC %md
# MAGIC ##### === Query ===

# COMMAND ----------

query = """
select 
  a.num_operacion,
  a.rut_cliente,
  a.dv_cliente,
  a.rut_deudor,
  a.dv_deudor,
  a.num_documento_fact,
  a.mto_saldo_ifrs,
  a.ind_segmentacion,
  a.cod_clasif_deudor,
  a.num_dias_mora,
  a.des_tramo_mora,
  a.num_operacion_fact,
  a.num_id_documento_fact,
  a.cod_responsabilidad_cedente,
  a.cod_castigo,
  a.cod_cart_deteriorada,
  a.pct_pi,
  a.pct_pdi,
  a.pct_pi_aval,
  a.pct_pdi_aval,
  a.mnt_provision,
  a.des_entidad,
  a.des_tipo_prestamo,
  a.des_matriz,
  a.pct_pe,
  a.pct_pe_aval,
  a.pct_factor_cubierto_aval,
  a.id_periodo,
  a.ind_negocio,
  a.des_tipo_mitigador,
  a.mnt_avalado,
  a.mnt_provision_con_mitigacion,
  a.fec_proceso
from """ + platinum_dbX + """.prv_fac_ft_b1_com_result a
WHERE fec_proceso = to_date('""" + fechanormativo + """', 'yyyy-MM-dd') 
"""
df = spark.sql(query)

# COMMAND ----------

# === Helpers ===
def rut12(col):  # 8 dígitos + padding a 12 con espacios
    return F.rpad(F.lpad(F.col(col).cast("string"), 8, '0'), 12, ' ')

def rpad12(col):  # padding a 12 con espacios
    return F.rpad(F.col(col).cast("string"), 12, ' ')

def fmt3(col):    # 3 decimales fijos
    return F.format_string("%.3f", F.col(col).cast("double"))

def trim_dec(col):  # elimina ceros de cola (1.000 -> 1 ; 0.028 queda igual)
    s = F.format_string("%.3f", F.col(col).cast("double"))
    s = F.regexp_replace(s, r"(\.\d*?[1-9])0+$", r"\1")
    s = F.regexp_replace(s, r"\.0+$", "")
    return s

def empty_to_null(col):
    c = F.col(col).cast("string")
    return F.when(c.isNull() | (F.trim(c) == ""), F.lit(None)).otherwise(c)

doc_col = F.col("num_documento_fact").cast("string") if not PAD_DOC else rpad12("num_documento_fact")

# COMMAND ----------

out = df.select(
    F.col("num_operacion").cast("string").alias("ope_num"),
    rut12("rut_cliente").alias("rut"),                     # <- **12**
    F.col("dv_cliente").cast("string").alias("dv"),
    rut12("rut_deudor").alias("rut_deudor_fact"),          # <- **12**
    F.col("dv_deudor").cast("string").alias("dv_deudor_fact"),
    doc_col.alias("doc_num_documento_fact"),  # <- sin padding
    F.col("mto_saldo_ifrs").cast("string").alias("Deuda_TotalIFRS"),
    F.col("ind_segmentacion").cast("string").alias("seg_n2"),
    F.col("cod_clasif_deudor").cast("string").alias("Clasificacion_Deudor_fact"),
    F.col("num_dias_mora").cast("string").alias("Dias_Mora"),
    F.col("des_tramo_mora").cast("string").alias("Tramo_Mora"),
    F.col("num_operacion_fact").cast("string").alias("doc_num_operacion_fact"),
    F.col("num_id_documento_fact").cast("string").alias("doc_id_documento_fact"),
    F.col("cod_responsabilidad_cedente").cast("string").alias("Responsabilidad_CedenteFactoring"),
    F.col("cod_castigo").cast("string").alias("Ind_Castigo"),
    F.col("cod_cart_deteriorada").cast("string").alias("Colo_EsCardetIrr"),
    fmt3("pct_pi").alias("PI"),
    fmt3("pct_pdi").alias("PDI"),
    fmt3("pct_pi_aval").alias("PI_A"),
    fmt3("pct_pdi_aval").alias("PDI_A"),
    fmt3("mnt_provision").alias("PROV_d00"),  
    F.col("des_entidad").cast("string").alias("Banco"),
    F.col("des_tipo_prestamo").cast("string").alias("Prestamo"),
    F.col("des_matriz").cast("string").alias("Matriz"),
    fmt3("pct_pe").alias("PE"),
    fmt3("pct_pe_aval").alias("PE_A"),
    trim_dec("pct_factor_cubierto_aval").alias("FACT_A"),
    F.col("id_periodo").cast("string").alias("periodo"),
    F.col("ind_negocio").cast("string").alias("Negocio_Id"),
    empty_to_null("des_tipo_mitigador").alias("tipo"),
    trim_dec("mnt_avalado").alias("Monto_Avalado"),
    fmt3("mnt_provision_con_mitigacion").alias("PROV_cm")
)

# COMMAND ----------

# MAGIC %md
# MAGIC ##### === Construir LÍNEAS MANUALMENTE y escribir como TEXTO ===

# COMMAND ----------

# 1) Cabecera
header_line = DELIM.join(out.columns) + "\r\n"

# 2) Filas (preservando espacios de padding)
line_exprs = [F.when(F.col(c).isNull(), F.lit("")).otherwise(F.col(c)) for c in out.columns]
lines_df = out.select(F.concat(F.concat_ws(DELIM, *line_exprs), F.lit("\r\n")).alias("value"))

# 3) Unir cabecera + filas
header_df = spark.createDataFrame([(header_line,)], ["value"])
all_lines = header_df.unionByName(lines_df)

# COMMAND ----------

# MAGIC %md
# MAGIC ##### === Función para limpiar ===

# COMMAND ----------

def rm_if_exists(path, recurse=True):
    try:
        dbutils.fs.rm(path, recurse=recurse)
    except Exception:
        pass

# COMMAND ----------

# MAGIC %md
# MAGIC ##### === Limpieza y escritura (usa .text para evitar CSV writer) ===

# COMMAND ----------

rm_if_exists(final_path, recurse=False)
rm_if_exists(tmp_dir, recurse=True)

# COMMAND ----------

(all_lines.coalesce(1)
         .write
         .mode("overwrite")
         .text(tmp_dir))

# COMMAND ----------

# MAGIC %md
# MAGIC ##### === Renombrar part-* a nombre final ===

# COMMAND ----------

part = [f.path for f in dbutils.fs.ls(tmp_dir) if re.search(r"/part-.*", f.path)]
if not part:
    raise RuntimeError("No se encontró el archivo part-* en el temporal.")
dbutils.fs.mv(part[0], final_path)
rm_if_exists(tmp_dir, recurse=True)

# COMMAND ----------

print(f"Archivo generado: {final_path}")

# COMMAND ----------

# MAGIC %md
# MAGIC ## Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")