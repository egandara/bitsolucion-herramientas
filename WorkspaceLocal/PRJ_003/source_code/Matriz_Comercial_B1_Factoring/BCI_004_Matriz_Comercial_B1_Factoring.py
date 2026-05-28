# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_004_Matriz_Comercial_B1_Factoring

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
dbutils.widgets.text("db_location_platinum_tempW","","03 Location DB Platinum Temp:")
dbutils.widgets.text("slv_RiesgoCred_RiesgoCredEmp_dbW","","04 RiesgoCred_RiesgoCredEmpdb:")
dbutils.widgets.text("slv_Clientes_Juridicas_dbW","","05 Clientes_Juridicas DB:")

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignar Objeto a Lectura de Widgets y Variables

# COMMAND ----------

fechaProcesoX = dbutils.widgets.get("fechaProcesoW")
spark.conf.set("bci.fechaProcesoX", fechaProcesoX)

platinum_temp_dbX = dbutils.widgets.get("platinum_temp_dbW")
spark.conf.set("bci.platinum_temp_dbX", platinum_temp_dbX)

db_location_platinum_tempX = dbutils.widgets.get("db_location_platinum_tempW")
spark.conf.set("bci.db_location_platinum_tempX", db_location_platinum_tempX)

slv_RiesgoCred_RiesgoCredEmp_dbX = dbutils.widgets.get("slv_RiesgoCred_RiesgoCredEmp_dbW")
spark.conf.set("bci.slv_RiesgoCred_RiesgoCredEmp_dbX", slv_RiesgoCred_RiesgoCredEmp_dbX)

slv_Clientes_Juridicas_dbX = dbutils.widgets.get("slv_Clientes_Juridicas_dbW")
spark.conf.set("bci.slv_Clientes_Juridicas_dbX", slv_Clientes_Juridicas_dbX)

print("*****Parámetros*****")
print("fechaProcesoX: " + fechaProcesoX)
print("platinum_temp_dbX: " + platinum_temp_dbX)
print("db_location_platinum_tempX: " + db_location_platinum_tempX)
print("slv_RiesgoCred_RiesgoCredEmp_dbX: " + slv_RiesgoCred_RiesgoCredEmp_dbX)
print("slv_Clientes_Juridicas_dbX: " + slv_Clientes_Juridicas_dbX)



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
valida_param_vacio(db_location_platinum_tempX,'db_location_platinum_tempX')

valida_param_vacio(slv_RiesgoCred_RiesgoCredEmp_dbX,'slv_RiesgoCred_RiesgoCredEmp_dbX')
valida_param_vacio(slv_Clientes_Juridicas_dbX,'slv_Clientes_Juridicas_dbX')

# COMMAND ----------

valida_bd(platinum_temp_dbX, 'platinum_temp_dbX')
valida_bd(slv_RiesgoCred_RiesgoCredEmp_dbX,'slv_RiesgoCred_RiesgoCredEmp_dbX')
valida_bd(slv_Clientes_Juridicas_dbX,'slv_Clientes_Juridicas_dbX')

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
# MAGIC ### Creación de tablas temporales que se eliminarán al final de la ejecución
# MAGIC
# MAGIC **************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla Origen: erc_prs_cba_hist

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una tabla temporal con los RUT de clientes que tienen balances auditados para el período de proceso.
# MAGIC
# MAGIC **Tabla de salida**: `dsr_plt_bcitemp_db.Tmp_erc_prs_cba_hist_rut`
# MAGIC
# MAGIC **variables de salida:**
# MAGIC * **mit_rut**: RUT del cliente utilizado para la mitigación.
# MAGIC * **Balances_Aud**: Indicador (1) de que el cliente presenta balances auditados.

# COMMAND ----------

drop_tmp_003_erc_prs_cba_hist_rut = """ DROP TABLE IF EXISTS """ + platinum_temp_dbX + """.Tmp_erc_prs_cba_hist_rut """

# COMMAND ----------

sql_safe(drop_tmp_003_erc_prs_cba_hist_rut)

# COMMAND ----------

dbutils.fs.rm(db_location_platinum_tempX+"Tmp_erc_prs_cba_hist_rut", True)

# COMMAND ----------

create_tmp_002_Tmp_CardetIrr = """ CREATE TABLE IF NOT EXISTS """ + platinum_temp_dbX + """.Tmp_erc_prs_cba_hist_rut 
USING DELTA
LOCATION '""" + db_location_platinum_tempX + """Tmp_erc_prs_cba_hist_rut'
AS (
select 
distinct cast(trim(pch_cli_rut) as integer) as mit_rut
,1 as Balances_Aud
from """ + slv_RiesgoCred_RiesgoCredEmp_dbX + """.erc_prs_cba_hist
where id_periodo = (select max(id_periodo) from """ + slv_RiesgoCred_RiesgoCredEmp_dbX + """.erc_prs_cba_hist)
)
"""

# COMMAND ----------

sql_safe(create_tmp_002_Tmp_CardetIrr)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla Origen: prv_rut_estatal

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una tabla temporal con los RUT de clientes que son entidades estatales y se encuentran vigentes.
# MAGIC
# MAGIC **Tabla de salida**: `dsr_plt_bcitemp_db.Tmp_prv_rut_estatal`
# MAGIC
# MAGIC **variables de salida:**
# MAGIC * **mit_rut**: RUT del cliente utilizado para la mitigación.
# MAGIC * **Rut_Estatal**: Indicador (1) de que el cliente es una entidad estatal.

# COMMAND ----------

drop_tmp_004_prv_rut_estatal = """ DROP TABLE IF EXISTS """ + platinum_temp_dbX + """.Tmp_prv_rut_estatal """

# COMMAND ----------

sql_safe(drop_tmp_004_prv_rut_estatal)

# COMMAND ----------

dbutils.fs.rm(db_location_platinum_tempX+"Tmp_prv_rut_estatal", True)

# COMMAND ----------

create_tmp_004_Tmp_prv_rut_estatal = """ CREATE TABLE IF NOT EXISTS """ + platinum_temp_dbX + """.Tmp_prv_rut_estatal 
USING DELTA
LOCATION '""" + db_location_platinum_tempX + """Tmp_prv_rut_estatal'
AS (
select 
distinct cast(trim(res_rut_cliente) as integer) as mit_rut
,1 AS Rut_Estatal
from """ +  slv_Clientes_Juridicas_dbX + """.prv_rut_estatal
WHERE trim(res_ind_vigencia)="S"
)
"""

# COMMAND ----------

sql_safe(create_tmp_004_Tmp_prv_rut_estatal)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Fundir tablas: Tmp_erc_prs_cba_hist_rut y Tmp_CardetIrr (002)

# COMMAND ----------

# MAGIC %md
# MAGIC Se une la tabla de cartera (`Tmp_CardetIrr`) con la de balances auditados para identificar operaciones con deudores que tienen mitigadores permitidos.
# MAGIC
# MAGIC **Tabla de salida**: `dsr_plt_bcitemp_db.Tmp_Fundir1_004`
# MAGIC
# MAGIC **variables de salida:**
# MAGIC * **mit_rut**: RUT del deudor, usado como clave para la unión.
# MAGIC * **(columnas de Tmp_CardetIrr)**: Todos los campos de la tabla de cartera.
# MAGIC * **Balances_Aud**: Indicador de si el deudor tiene balances auditados.

# COMMAND ----------

drop_tmp_005_Fundir1_004 = """ DROP TABLE IF EXISTS """ + platinum_temp_dbX + """.Tmp_Fundir1_004 """

# COMMAND ----------

sql_safe(drop_tmp_005_Fundir1_004)

# COMMAND ----------

dbutils.fs.rm(db_location_platinum_tempX+"Tmp_Fundir1_004", True)

# COMMAND ----------

create_tmp_005_Fundir1_004 = """ CREATE TABLE IF NOT EXISTS """ + platinum_temp_dbX + """.Tmp_Fundir1_004 
USING DELTA
LOCATION '""" + db_location_platinum_tempX + """Tmp_Fundir1_004'
AS (
select cast(trim(a.ddr_idc) as integer) AS mit_rut
,a.dpf_fec_proceso
,a.cli_idc
,a.cli_vrt
,a.cli_rzn_soc
,a.ddr_idc
,a.ddr_vrt
,a.ddr_rzn_soc
,a.pdt_des_cra
,a.doc_num_documento
,a.doc_num_cuota
,a.Deuda_TotalIFRS
,a.dod_cod_cobranza
,a.cod_aec
,a.dpf_ind_responsabilidad
,a.dpf_fec_vencimiento
,a.Clasificacion
,a.dod_ind_cartera
,a.Clasificacion_Cliente
,a.Clasificacion_Deudor
,a.Dias_Mora
,a.doc_num_operacion
,a.doc_id_documento
,a.doc_ind_fin_mes
,a.dpf_pct_pi
,a.dpf_pct_pdi
,a.dpf_mto_pe
,a.cli_rut
,a.Responsabilidad_CedenteFactoring
,a.Tipo_Prestamo
,a.Periodo_Id
,a.Ind_Castigo
,a.Colo_EsCardetIrr
,a.Cod_niid
,a.Calif_Deudor_Permitida
,b.Balances_Aud
FROM """+platinum_temp_dbX+""".Tmp_CardetIrr a
left join """+platinum_temp_dbX+""".Tmp_erc_prs_cba_hist_rut b
on cast(trim(a.ddr_idc) as integer) = b.mit_rut
WHERE Calif_Deudor_Permitida=1 AND trim(pdt_des_cra) <> "RE"
)
"""

# COMMAND ----------

sql_safe(create_tmp_005_Fundir1_004)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Fundir Tmp_Fundir2_004

# COMMAND ----------

# MAGIC %md
# MAGIC Se calculan los valores de PI y PDI con mitigador (aval) para las operaciones que cumplen con tener balances auditados o ser entidad estatal.
# MAGIC
# MAGIC **Tabla de salida**: `dsr_plt_bcitemp_db.Tmp_Fundir2_004`
# MAGIC
# MAGIC **variables de salida:**
# MAGIC * **Cod_niid**: ID único de la operación para vincular los resultados.
# MAGIC * **PI_A**: Probabilidad de Incumplimiento ajustada por el aval/mitigador.
# MAGIC * **PDI_A**: Pérdida Dado el Incumplimiento ajustada por el aval/mitigador.
# MAGIC * **Mitigador**: Indicador (1) de que se aplicó un mitigador a la operación.

# COMMAND ----------

drop_tmp_006_Fundir2_004 = """ DROP TABLE IF EXISTS """ + platinum_temp_dbX + """.Tmp_Fundir2_004 """

# COMMAND ----------

sql_safe(drop_tmp_006_Fundir2_004)

# COMMAND ----------

dbutils.fs.rm(db_location_platinum_tempX+"Tmp_Fundir2_004", True)

# COMMAND ----------

create_tmp_005_Fundir2_004 = """ CREATE TABLE IF NOT EXISTS """ + platinum_temp_dbX + """.Tmp_Fundir2_004 
USING DELTA
LOCATION '""" + db_location_platinum_tempX + """Tmp_Fundir2_004'
AS (
select
a.Cod_niid
,CASE WHEN trim(a.Clasificacion_Deudor)="A1" then 0.0004
WHEN trim(a.Clasificacion_Deudor)="A2" then 0.001
WHEN trim(a.Clasificacion_Deudor)="A3" then 0.0025
ELSE 0 END AS PI_A
,CASE WHEN trim(a.Clasificacion_Deudor)="A1" then 0.9
WHEN trim(a.Clasificacion_Deudor)="A2" then 0.825
WHEN trim(a.Clasificacion_Deudor)="A3" then 0.875
ELSE 0 END AS PDI_A
,1 AS Mitigador
from """+platinum_temp_dbX+""".Tmp_Fundir1_004 a
left join """+platinum_temp_dbX+""".Tmp_prv_rut_estatal b
on a.mit_rut = b.mit_rut
WHERE IFNULL(a.Balances_Aud,0) = 1 or IFNULL(b.Rut_Estatal,0) = 1
)
"""

# COMMAND ----------

sql_safe(create_tmp_005_Fundir2_004)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Fundir PROV_d00

# COMMAND ----------

# MAGIC %md
# MAGIC Se consolida la información de provisiones, uniendo los datos de la cartera (`Tmp_Pi_Pdi`) con los mitigadores (`Tmp_Fundir2_004`) para obtener la provisión final.
# MAGIC
# MAGIC **Tabla de salida**: `dsr_plt_bcitemp_db.Tmp_Fundir_Prov_d00`
# MAGIC
# MAGIC **variables de salida:**
# MAGIC * **Cod_niid**: ID único de la operación.
# MAGIC * **(columnas de Tmp_Pi_Pdi)**: Todos los campos de la tabla de PI/PDI.
# MAGIC * **PI**: PI sin mitigador.
# MAGIC * **PDI**: PDI sin mitigador.
# MAGIC * **PI_A**: PI con mitigador (si aplica).
# MAGIC * **PDI_A**: PDI con mitigador (si aplica).
# MAGIC * **Mitigador**: Flag que indica si se usó mitigador.
# MAGIC * **PROV_d00**: Monto final de la provisión calculada.

# COMMAND ----------

drop_tmp_007_Fundir_Prov_d00 = """ DROP TABLE IF EXISTS """ + platinum_temp_dbX + """.Tmp_Fundir_Prov_d00 """

# COMMAND ----------

sql_safe(drop_tmp_007_Fundir_Prov_d00)

# COMMAND ----------

dbutils.fs.rm(db_location_platinum_tempX+"Tmp_Fundir_Prov_d00", True)

# COMMAND ----------

create_tmp_007_Fundir_Prov_d00 = """ CREATE TABLE IF NOT EXISTS """ + platinum_temp_dbX + """.Tmp_Fundir_Prov_d00 
USING DELTA
PARTITIONED BY (dpf_fec_proceso)
LOCATION '""" + db_location_platinum_tempX + """Tmp_Fundir_Prov_d00'
AS (
select
IFNULL(a.Cod_niid,b.Cod_niid) AS Cod_niid
,a.dpf_fec_proceso
,a.cli_idc
,a.cli_vrt
,a.cli_rzn_soc
,a.ddr_idc
,a.ddr_vrt
,a.ddr_rzn_soc
,a.pdt_des_cra
,a.doc_num_documento
,a.doc_num_cuota
,a.Deuda_TotalIFRS
,a.dod_cod_cobranza
,a.cod_aec
,a.dpf_ind_responsabilidad
,a.dpf_fec_vencimiento
,a.Clasificacion
,a.dod_ind_cartera
,a.Clasificacion_Cliente
,a.Clasificacion_Deudor
,a.Dias_Mora
,a.doc_num_operacion
,a.doc_id_documento
,a.doc_ind_fin_mes
,a.dpf_pct_pi
,a.dpf_pct_pdi
,a.dpf_mto_pe
,a.cli_rut
,a.Responsabilidad_CedenteFactoring
,a.Tipo_Prestamo
,a.Periodo_Id
,a.Ind_Castigo
,a.Colo_EsCardetIrr
,a.Calif_Deudor_Permitida
,IFNULL(a.PI,0) AS PI
,IFNULL(a.PDI,0) AS PDI
,IFNULL(b.PI_A,0) AS PI_A
,IFNULL(b.PDI_A,0) AS PDI_A
,IFNULL(b.Mitigador,0) AS Mitigador
, CASE WHEN Mitigador=1 then IFNULL(b.PI_A,0)*IFNULL(b.PDI_A,0)*Deuda_TotalIFRS
ELSE IFNULL(a.PI,0)*IFNULL(a.PDI,0)*Deuda_TotalIFRS END AS PROV_d00
from """+platinum_temp_dbX+""".Tmp_Pi_Pdi a
full join """+platinum_temp_dbX+""".Tmp_Fundir2_004 b
on trim(a.Cod_niid)=(b.Cod_niid)
)
"""

# COMMAND ----------

sql_safe(create_tmp_007_Fundir_Prov_d00)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")