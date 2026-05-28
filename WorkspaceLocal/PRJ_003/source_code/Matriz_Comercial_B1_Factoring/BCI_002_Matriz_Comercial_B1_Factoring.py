# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_002_Matriz_Comercial_B1_Factoring

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
print("db_location_platinum_tempX: " + db_location_platinum_tempX)

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

# MAGIC %md Se crea una tabla temporal con información de Factoring para clientes grupales, excluyendo operaciones castigadas.
# MAGIC
# MAGIC **Tabla de salida**: dsr_plt_normativo_db.Tmp_CardetIrr
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **dpf_fec_proceso**: Fecha del proceso en formato SSAAMMDD.
# MAGIC - **cli_idc**: Identificador numérico del cliente.
# MAGIC - **cli_vrt**: Dígito verificador del cliente.
# MAGIC - **cli_rzn_soc**: Razón social del cliente.
# MAGIC - **ddr_idc**: Identificador numérico del deudor.
# MAGIC - **ddr_vrt**: Dígito verificador del deudor.
# MAGIC - **ddr_rzn_soc**: Razón social del deudor.
# MAGIC - **pdt_des_cra**: Descripción de la cartera del producto.
# MAGIC - **doc_num_documento**: Número del documento.
# MAGIC - **doc_num_cuota**: Número de la cuota.
# MAGIC - **Deuda_TotalIFRS**: Monto del saldo de cierre de la operación.
# MAGIC - **dod_cod_cobranza**: Código del estado de cobranza.
# MAGIC - **cod_aec**: Código AEC de la operación.
# MAGIC - **dpf_ind_responsabilidad**: Indicador de responsabilidad de la operación.
# MAGIC - **dpf_fec_vencimiento**: Fecha de vencimiento de la cuota.
# MAGIC - **Clasificacion**: Clasificación del segmento ('G' para Grupal, 'I' para Individual).
# MAGIC - **dod_ind_cartera**: Indicador del tipo de cartera.
# MAGIC - **Clasificacion_Cliente**: Calificación crediticia del cliente.
# MAGIC - **Clasificacion_Deudor**: Calificación crediticia del deudor.
# MAGIC - **Dias_Mora**: Número de días de mora de la cuota.
# MAGIC - **doc_num_operacion**: Número de la operación de factoring.
# MAGIC - **doc_id_documento**: ID único del documento.
# MAGIC - **doc_ind_fin_mes**: Indicador de fin de mes.
# MAGIC - **dpf_pct_pi**: Porcentaje de Provisión Individual.
# MAGIC - **dpf_pct_pdi**: Porcentaje de Provisión de Deterioro Individual.
# MAGIC - **dpf_mto_pe**: Monto de la Pérdida Esperada.
# MAGIC - **cli_rut**: RUT numérico del cliente.
# MAGIC - **Responsabilidad_CedenteFactoring**: Indicador binario si la responsabilidad es del Cedente.
# MAGIC - **Tipo_Prestamo**: Tipo de préstamo, valor constante 'FACTORING'.
# MAGIC - **Periodo_Id**: Primer día del mes del proceso (formato AAAA-MM-DD).
# MAGIC - **Ind_Castigo**: Indicador binario si la cartera está castigada.
# MAGIC - **Colo_EsCardetIrr**: Indicador binario si la cartera es de Incumplimiento.
# MAGIC - **Cod_niid**: Código único de la operación.
# MAGIC - **Calif_Deudor_Permitida**: Indicador binario si la calificación del deudor es A1, A2 o A3.

# COMMAND ----------

drop_tmp_002_CardetIrr = """ DROP TABLE IF EXISTS """ + platinum_temp_dbX + """.Tmp_CardetIrr """

# COMMAND ----------

sql_safe(drop_tmp_002_CardetIrr)

# COMMAND ----------

dbutils.fs.rm(db_location_platinum_tempX+"Tmp_CardetIrr", True)

# COMMAND ----------

create_tmp_002_Tmp_CardetIrr = """ CREATE TABLE IF NOT EXISTS """ + platinum_temp_dbX + """.Tmp_CardetIrr 
USING DELTA
PARTITIONED BY (dpf_fec_proceso)
LOCATION '""" + db_location_platinum_tempX + """Tmp_CardetIrr'
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
,Monto_Saldo_Cierre AS Deuda_TotalIFRS
,dod_cod_cobranza
,cod_aec
,dpf_ind_responsabilidad
,dpf_fec_vencimiento
,Clasificacion
,dod_ind_cartera
,IFNULL(dpf_des_cal_cliente,"") AS Clasificacion_Cliente
,IFNULL(dpf_des_cal_deudor,"") AS Clasificacion_Deudor
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
,CASE WHEN trim(dod_ind_cartera)="INCUMPLIMIENTO" then 1 
else 0 END AS Colo_EsCardetIrr
, trim(cli_idc) || trim(cli_vrt) || trim(ddr_idc) || trim(ddr_vrt) || trim(pdt_des_cra) 
|| cast(doc_num_documento as string) || cast(doc_num_cuota as string) || cast(doc_num_operacion as string) AS Cod_niid
, CASE WHEN trim(Clasificacion_Deudor) ="A1" or trim(Clasificacion_Deudor) ="A2" or trim(Clasificacion_Deudor) ="A3" THEN 1
ELSE 0 END AS Calif_Deudor_Permitida
from """ + platinum_temp_dbX + """.Tmp_Factoring_Nodo
where Ind_Castigo=0 AND trim(Clasificacion)="G"
)
"""

# COMMAND ----------

sql_safe(create_tmp_002_Tmp_CardetIrr)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")