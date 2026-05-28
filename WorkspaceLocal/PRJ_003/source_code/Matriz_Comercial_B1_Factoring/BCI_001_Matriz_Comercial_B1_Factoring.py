# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_001_Matriz_Comercial_B1_Factoring

# COMMAND ----------

# MAGIC %md
# MAGIC ## Información del Notebook

# COMMAND ----------

# MAGIC %md
# MAGIC ### Encabezado
# MAGIC **************************************************************************
# MAGIC * Nombre: BCI_001_Factoring.ipynb
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
dbutils.widgets.text("slv_ProdServicios_Factoring_dbW","","04 ProdServicios_Factoring DB:")

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

slv_ProdServicios_Factoring_dbX = dbutils.widgets.get("slv_ProdServicios_Factoring_dbW")
spark.conf.set("bci.slv_ProdServicios_Factoring_dbX", slv_ProdServicios_Factoring_dbX)

print("*****Parámetros*****")
print("fechaProcesoX: " + fechaProcesoX)
print("platinum_temp_dbX: " + platinum_temp_dbX)
print("db_location_platinum_tempX: " + db_location_platinum_tempX)
print("slv_ProdServicios_Factoring_dbX: " + slv_ProdServicios_Factoring_dbX)


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
valida_param_vacio(slv_ProdServicios_Factoring_dbX,'slv_ProdServicios_Factoring_dbX')

# COMMAND ----------

valida_bd(platinum_temp_dbX, 'platinum_temp_dbX')
valida_bd(slv_ProdServicios_Factoring_dbX, 'slv_ProdServicios_Factoring_dbX')

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
# MAGIC #### Tabla dpf_documento_factoring

# COMMAND ----------

# MAGIC %md
# MAGIC ### Creación de tablas temporales que se eliminarán al final de la ejecución
# MAGIC
# MAGIC **************************************************************************

# COMMAND ----------

drop_tmp_001_Factoring_Nodo = """ DROP TABLE IF EXISTS """ + platinum_temp_dbX + """.Tmp_Factoring_Nodo """

# COMMAND ----------

sql_safe(drop_tmp_001_Factoring_Nodo)

# COMMAND ----------

dbutils.fs.rm(db_location_platinum_tempX+"Tmp_Factoring_Nodo", True)

# COMMAND ----------

# MAGIC %md
# MAGIC ### **Tabla de salida: dsr_plt_bcitemp_db.Tmp_Factoring_Nodo**
# MAGIC
# MAGIC **Variables de salida:**
# MAGIC
# MAGIC * dpf_fec_proceso: Fecha del proceso en formato SSAAMMDD.
# MAGIC * cli_idc: Identificador numérico del cliente.
# MAGIC * cli_vrt: Dígito verificador del cliente.
# MAGIC * cli_rzn_soc: Razón social o nombre del cliente.
# MAGIC * ddr_idc: Identificador numérico del deudor.
# MAGIC * ddr_vrt: Dígito verificador del deudor.
# MAGIC * ddr_rzn_soc: Razón social o nombre del deudor.
# MAGIC * pdt_des_cra: Descripción de la cartera del producto.
# MAGIC * doc_num_documento: Número identificador del documento.
# MAGIC * doc_num_cuota: Número de la cuota dentro del documento.
# MAGIC * Monto_Saldo_Cierre: Monto del saldo de la operación al cierre.
# MAGIC * dod_cod_cobranza: Código del estado de cobranza de la operación.
# MAGIC * cod_aec: Código AEC de la operación.
# MAGIC * dpf_ind_responsabilidad: Indicador que define la responsabilidad de la operación.
# MAGIC * dpf_fec_vencimiento: Fecha de vencimiento de la cuota.
# MAGIC * Clasificacion: Clasifica el segmento en 'G' (Grupal) o 'I' (Individual).
# MAGIC * dod_ind_cartera: Indicador que define el tipo de cartera.
# MAGIC * dpf_des_cal_cliente: Descripción de la calificación crediticia del cliente.
# MAGIC * dpf_des_cal_deudor: Descripción de la calificación crediticia del deudor.
# MAGIC * Dias_Mora: Número de días de atraso de la cuota.
# MAGIC * doc_num_operacion: Número de la operación de factoring.
# MAGIC * doc_id_documento: ID único del documento.
# MAGIC * doc_ind_fin_mes: Indicador que marca si el registro es de fin de mes.
# MAGIC * dpf_pct_pi: Porcentaje de Provisión Individual.
# MAGIC * dpf_pct_pdi: Porcentaje de Provisión de Deterioro Individual.
# MAGIC * dpf_mto_pe: Monto de la Pérdida Esperada.
# MAGIC * cli_rut: RUT numérico del cliente, sin dígito verificador.
# MAGIC * Responsabilidad_CedenteFactoring: Indicador binario (1/0) si la responsabilidad es del Cedente.
# MAGIC * Tipo_Prestamo: Campo constante con el valor 'FACTORING'.
# MAGIC * Periodo_Id: Fecha del primer día del mes del proceso (formato AAAA-MM-DD).
# MAGIC * Ind_Castigo: Indicador binario (1/0) si la cartera está marcada como Castigada.

# COMMAND ----------

create_tmp_001_Factoring_Nodo = """ CREATE TABLE """ + platinum_temp_dbX + """.Tmp_Factoring_Nodo 
USING DELTA
PARTITIONED BY (dpf_fec_proceso)
LOCATION '""" + db_location_platinum_tempX + """Tmp_Factoring_Nodo'
AS (
SELECT TRIM(dpf_fec_proceso) AS dpf_fec_proceso,
TRIM(cli_idc) AS cli_idc,
TRIM(cli_vrt) AS cli_vrt,
TRIM(cli_rzn_soc) AS cli_rzn_soc,
TRIM(ddr_idc) AS ddr_idc,
TRIM(ddr_vrt) AS ddr_vrt,
TRIM(ddr_rzn_soc) AS ddr_rzn_soc,
TRIM(pdt_des_cra) AS pdt_des_cra,
TRIM(doc_num_documento) AS doc_num_documento,
TRIM(doc_num_cuota) AS doc_num_cuota,
TRIM(dod_mto_saldo_cierre) AS Monto_Saldo_Cierre,
TRIM(dod_cod_cobranza) AS dod_cod_cobranza,
TRIM(cod_aec) AS cod_aec,
TRIM(dpf_ind_responsabilidad) AS dpf_ind_responsabilidad,
TRIM(dpf_fec_vencimiento) AS dpf_fec_vencimiento,
CASE WHEN TRIM(dod_ind_segmento) = 'GRUPAL' THEN 'G'
WHEN TRIM(dod_ind_segmento) = 'INDIVIDUAL' THEN 'I'
ELSE TRIM(dod_ind_segmento) END AS Clasificacion,
TRIM(dod_ind_cartera) AS dod_ind_cartera,
TRIM(dpf_des_cal_cliente) AS dpf_des_cal_cliente,
TRIM(dpf_des_cal_deudor) AS dpf_des_cal_deudor,
TRIM(dod_num_dias) AS Dias_Mora,
TRIM(doc_num_operacion) AS doc_num_operacion,
TRIM(doc_id_documento) AS doc_id_documento,
TRIM(doc_ind_fin_mes) AS doc_ind_fin_mes,
TRIM(dpf_pct_pi) AS dpf_pct_pi,
TRIM(dpf_pct_pdi) AS dpf_pct_pdi,
TRIM(dpf_mto_pe) AS dpf_mto_pe,
cast(TRIM(cli_idc) as integer) as cli_rut,
CASE WHEN TRIM(dpf_ind_responsabilidad) = 'C' THEN 1
ELSE 0 END AS Responsabilidad_CedenteFactoring,
'FACTORING' AS Tipo_Prestamo,
substr(dpf_fec_proceso,1,4)|| '-' ||substr(dpf_fec_proceso,6,2)|| '-' ||'01' AS Periodo_Id,
CASE WHEN TRIM(pdt_des_cra) = 'CG' THEN 1
ELSE 0 END AS Ind_Castigo
FROM """ + slv_ProdServicios_Factoring_dbX + """.dpf_documento_factoring
where dpf_fec_proceso = '"""+anomesdia2+"""'
)
"""

# COMMAND ----------

sql_safe(create_tmp_001_Factoring_Nodo)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")