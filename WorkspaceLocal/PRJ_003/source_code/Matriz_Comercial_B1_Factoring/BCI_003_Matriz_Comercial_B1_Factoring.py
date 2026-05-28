# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_003_Matriz_Comercial_B1_Factoring

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

print("*****Parámetros*****")
print("fechaProcesoX: " + fechaProcesoX)
print("platinum_temp_dbX: " + platinum_temp_dbX)
print("db_location_platinum_tempW: " + db_location_platinum_tempX)

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

# COMMAND ----------

valida_bd(platinum_temp_dbX, 'platinum_temp_dbX')

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
# MAGIC Se crea la tabla temporal para PI (Probabilidad de Incumplimiento) y PDI (Pérdida Dado el Incumplimiento) a partir de la cartera. Los valores de PI se asignan según los días de mora.
# MAGIC
# MAGIC **Tabla de salida**: `dsr_plt_bcitemp_db.Tmp_Pi_Pdi`
# MAGIC
# MAGIC **variables de salida:**
# MAGIC * **dpf_fec_proceso**: Fecha del procesamiento de los datos.
# MAGIC * **cli_idc**: RUT del cliente.
# MAGIC * **cli_vrt**: DV del RUT del cliente.
# MAGIC * **cli_rzn_soc**: Razón social del cliente.
# MAGIC * **ddr_idc**: RUT del deudor.
# MAGIC * **ddr_vrt**: DV del RUT del deudor.
# MAGIC * **ddr_rzn_soc**: Razón social del deudor.
# MAGIC * **pdt_des_cra**: Descripción del tipo de documento.
# MAGIC * **doc_num_documento**: Número del documento.
# MAGIC * **doc_num_cuota**: Número de la cuota.
# MAGIC * **Deuda_TotalIFRS**: Monto de la deuda bajo norma IFRS.
# MAGIC * **dod_cod_cobranza**: Código del estado de cobranza.
# MAGIC * **cod_aec**: Código de actividad económica.
# MAGIC * **dpf_ind_responsabilidad**: Indicador de responsabilidad.
# MAGIC * **dpf_fec_vencimiento**: Fecha de vencimiento.
# MAGIC * **Clasificacion**: Clasificación del crédito.
# MAGIC * **dod_ind_cartera**: Indicador del tipo de cartera.
# MAGIC * **Clasificacion_Cliente**: Clasificación crediticia del cliente.
# MAGIC * **Clasificacion_Deudor**: Clasificación crediticia del deudor.
# MAGIC * **Dias_Mora**: Cantidad de días de mora.
# MAGIC * **doc_num_operacion**: Número de la operación.
# MAGIC * **doc_id_documento**: ID del documento.
# MAGIC * **doc_ind_fin_mes**: Indicador de fin de mes.
# MAGIC * **dpf_pct_pi**: PI original.
# MAGIC * **dpf_pct_pdi**: PDI original.
# MAGIC * **dpf_mto_pe**: Monto de Pérdida Esperada original.
# MAGIC * **cli_rut**: RUT completo del cliente.
# MAGIC * **Responsabilidad_CedenteFactoring**: Indicador de responsabilidad del cedente.
# MAGIC * **Tipo_Prestamo**: Tipo de préstamo.
# MAGIC * **Periodo_Id**: ID del período (SSAAMM).
# MAGIC * **Ind_Castigo**: Indicador de castigo.
# MAGIC * **Colo_EsCardetIrr**: Indicador de cartera deteriorada.
# MAGIC * **Cod_niid**: ID único de la operación.
# MAGIC * **Calif_Deudor_Permitida**: Indicador de calificación permitida del deudor.
# MAGIC * **PI**: Probabilidad de Incumplimiento calculada.
# MAGIC * **PDI**: Pérdida Dado el Incumplimiento calculada.

# COMMAND ----------

drop_tmp_003_Tmp_Pi_Pdi = """ DROP TABLE IF EXISTS """ + platinum_temp_dbX + """.Tmp_Pi_Pdi """

# COMMAND ----------

sql_safe(drop_tmp_003_Tmp_Pi_Pdi)

# COMMAND ----------

dbutils.fs.rm(db_location_platinum_tempX+"Tmp_Pi_Pdi", True)

# COMMAND ----------

create_tmp_003_Tmp_Pi_Pdi = """ CREATE TABLE IF NOT EXISTS """ + platinum_temp_dbX + """.Tmp_Pi_Pdi 
USING DELTA
PARTITIONED BY (dpf_fec_proceso)
LOCATION '""" + db_location_platinum_tempX + """Tmp_Pi_Pdi'
AS (
select 
dpf_fec_proceso
,cli_idc
,cli_vrt
,cli_rzn_soc
,ddr_idc
,ddr_vrt
,ddr_rzn_soc
,pdt_des_cra
,doc_num_documento
,doc_num_cuota
,IFNULL(Deuda_TotalIFRS, 0) AS Deuda_TotalIFRS
,dod_cod_cobranza
,cod_aec
,dpf_ind_responsabilidad
,dpf_fec_vencimiento
,Clasificacion
,dod_ind_cartera
,Clasificacion_Cliente
,Clasificacion_Deudor
,Dias_Mora
,doc_num_operacion
,doc_id_documento
,doc_ind_fin_mes
,dpf_pct_pi
,dpf_pct_pdi
,dpf_mto_pe
,cli_rut
,Responsabilidad_CedenteFactoring
,Tipo_Prestamo
,Periodo_Id
,Ind_Castigo
,Colo_EsCardetIrr
,Cod_niid
,Calif_Deudor_Permitida
,CASE WHEN Dias_Mora=0 and Colo_EsCardetIrr=0 then 0.0491
WHEN Dias_Mora>=1 and Dias_Mora<=29 and Colo_EsCardetIrr=0 then 0.2293
WHEN Dias_Mora>=30 and Dias_Mora<=59 and Colo_EsCardetIrr=0 then 0.4530
WHEN Dias_Mora>=60 and Dias_Mora<=89 and Colo_EsCardetIrr=0 then 0.6163
ELSE 1 END AS PI
,0.359 as PDI
from """+platinum_temp_dbX+""".Tmp_CardetIrr
where Responsabilidad_CedenteFactoring=1
)
"""

# COMMAND ----------

sql_safe(create_tmp_003_Tmp_Pi_Pdi)

# COMMAND ----------

insert_001_Tmp_Pi_Pdi = """ INSERT INTO """ + platinum_temp_dbX + """.Tmp_Pi_Pdi 
select 
dpf_fec_proceso
,cli_idc
,cli_vrt
,cli_rzn_soc
,ddr_idc
,ddr_vrt
,ddr_rzn_soc
,pdt_des_cra
,doc_num_documento
,doc_num_cuota
,IFNULL(Deuda_TotalIFRS, 0) AS Deuda_TotalIFRS
,dod_cod_cobranza
,cod_aec
,dpf_ind_responsabilidad
,dpf_fec_vencimiento
,Clasificacion
,dod_ind_cartera
,Clasificacion_Cliente
,Clasificacion_Deudor
,Dias_Mora
,doc_num_operacion
,doc_id_documento
,doc_ind_fin_mes
,dpf_pct_pi
,dpf_pct_pdi
,dpf_mto_pe
,cli_rut
,Responsabilidad_CedenteFactoring
,Tipo_Prestamo
,Periodo_Id
,Ind_Castigo
,Colo_EsCardetIrr
,Cod_niid
,Calif_Deudor_Permitida
,CASE WHEN Dias_Mora=0 and Colo_EsCardetIrr=0 then 0.0491
WHEN Dias_Mora>=1 and Dias_Mora<=29 and Colo_EsCardetIrr=0 then 0.2293
WHEN Dias_Mora>=30 and Dias_Mora<=59 and Colo_EsCardetIrr=0 then 0.4530
WHEN Dias_Mora>=60 and Dias_Mora<=89 and Colo_EsCardetIrr=0 then 0.6163
ELSE 1 END AS PI
,0.569 AS PDI
from """+platinum_temp_dbX+""".Tmp_CardetIrr
where Responsabilidad_CedenteFactoring=0
"""

# COMMAND ----------

sql_safe(insert_001_Tmp_Pi_Pdi)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")