# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_009_Base_Normativa_Operaciones_Leasing_Comercial

# COMMAND ----------

# MAGIC %md
# MAGIC ## Información del Notebook

# COMMAND ----------

# MAGIC %md
# MAGIC ### Encabezado
# MAGIC **************************************************************************
# MAGIC * Nombre: BCI_009_Base_Normativa_Operaciones_Leasing_Comercial.ipynb
# MAGIC * Ruta: 
# MAGIC * Autor:  BitSolucion Spa
# MAGIC * Ing. SW BCI: <alguien.de.ti@bci.cl> - Marcelo Lizama <marcelo.lizama@bci.cl> - Ivan Jara <ivan.jara@bci.cl>
# MAGIC * Fecha: 22/12/2025
# MAGIC * Descripción: Notebook encargado de insertar información de Operaciones Leasing de Colocaciones Comerciales Grupal e Individual, Tipos de Activo 13
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
# MAGIC #### Tabla Entrada ADLS: 
# MAGIC * tmp_modelo_indiv
# MAGIC * tmp_d00_deuda_act_ctg
# MAGIC * tmp_modelo_com_gr
# MAGIC ***************************************************************************
# MAGIC #### Tabla Salida Temporal: 
# MAGIC * base_archivos_normativos (Activo 13 Tabla 89 Manual CMF)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Captura de Variables

# COMMAND ----------

# MAGIC %md
# MAGIC ### Crear Widgets para Captura de Variables

# COMMAND ----------

dbutils.widgets.removeAll()
dbutils.widgets.text("fechaProcesoW","","01 Fecha Proceso :")
dbutils.widgets.text("db_platinumW","","02 platinum DB:")
dbutils.widgets.text("db_plat_tempW","","03 platinum temp db:")
dbutils.widgets.text("db_location_plat_tempW","","04 Location platinum temp db:")
dbutils.widgets.text("db_RiesgoCredW","","05 slv_RiesgoCred_RiesgoCredPer DB:")
dbutils.widgets.text("db_slv_ParametricasW","","06 Parametricas DB:")

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignar Objeto a Lectura de Widgets y Variables

# COMMAND ----------

fechaProcesoX = dbutils.widgets.get("fechaProcesoW")
spark.conf.set("bci.fechaProcesoX", fechaProcesoX)

db_platinumX = dbutils.widgets.get("db_platinumW")
spark.conf.set("bci.db_platinumX", db_platinumX)

db_plat_tempX = dbutils.widgets.get("db_plat_tempW")
spark.conf.set("bci.db_plat_tempX", db_plat_tempX)

db_location_plat_tempX = dbutils.widgets.get("db_location_plat_tempW")
spark.conf.set("bci.db_location_plat_tempX", db_location_plat_tempX)

db_RiesgoCredX = dbutils.widgets.get("db_RiesgoCredW")
spark.conf.set("bci.db_RiesgoCredX", db_RiesgoCredX)

db_slv_ParametricasX = dbutils.widgets.get("db_slv_ParametricasW")
spark.conf.set("bci.db_slv_ParametricasX", db_slv_ParametricasX)

print("fechaProcesoX: " + fechaProcesoX)
print("db_platinumX: " + db_platinumX)
print("db_plat_tempX: " + db_plat_tempX)
print("db_location_plat_tempX: " + db_location_plat_tempX)
print("db_RiesgoCredX: " + db_RiesgoCredX)
print("db_slv_ParametricasX: " + db_slv_ParametricasX)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Carga de funciones

# COMMAND ----------

# MAGIC %run "../../Funciones"

# COMMAND ----------

# MAGIC %md
# MAGIC ## Validación de ingreso de parámetros

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Parametro Vacio

# COMMAND ----------

valida_param_vacio(fechaProcesoX,'fechaProcesoX')
valida_param_vacio(db_platinumX,'db_platinumX')
valida_param_vacio(db_plat_tempX,'db_plat_tempX')
valida_param_vacio(db_RiesgoCredX,'db_RiesgoCredX')
valida_param_vacio(db_location_plat_tempX,'db_location_plat_tempX')
valida_param_vacio(db_slv_ParametricasX,'db_slv_ParametricasX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Base de Datos

# COMMAND ----------

valida_bd(db_platinumX, 'db_platinumX')
valida_bd(db_slv_ParametricasX, 'db_slv_ParametricasX')
valida_bd(db_plat_tempX, 'db_plat_tempX')
valida_bd(db_RiesgoCredX, 'db_RiesgoCredX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Fecha Valida

# COMMAND ----------

valida_fecha_valida(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Fecha Futura

# COMMAND ----------

valida_fecha_futura(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Ruta 

# COMMAND ----------

valida_ruta(db_location_plat_tempX, 'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC ## Asigan Variables de fecha

# COMMAND ----------

ano = str(fechaProcesoX)[:4]
mes = str(fechaProcesoX)[4:][:2]
dia = str(fechaProcesoX)[6:][:2]
fechanormativo = str(ano+'-'+mes+'-'+dia)
fechacinta = str(dia+'-'+mes+'-'+ano)
anomes = str(ano+mes)
anomesdia = str(ano+mes+dia)

print("fecha_Formato1: " + ano)
print("fecha_Formato2: " + mes)
print("fecha_Formato3: " + dia)
print("fecha_Formato4: " + fechanormativo)
print("fecha_Formato5: " + fechacinta)
print("fecha_Formato6: " + anomes)
print("fecha_Formato7: " + anomesdia)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Inicio Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ## Creacion tablas temporales

# COMMAND ----------

# MAGIC %md
# MAGIC ###Tabla tmp_modelo_indv_leasing

# COMMAND ----------

paso_tb_del_tmp_modelo_indv_lea_rep  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_modelo_indv_leasing"""

# COMMAND ----------

sql_safe(paso_tb_del_tmp_modelo_indv_lea_rep)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_modelo_indv_leasing", True)

# COMMAND ----------

create_tmp_modelo_indv_lea = """CREATE TABLE """ + db_plat_tempX + """.tmp_modelo_indv_leasing
(
  periodo_id INT COMMENT 'Periodo de ejecucion SSAAMM',
  rut_cliente INT COMMENT 'rut cliente',
  dv_cliente STRING COMMENT 'dígito verificador cliente',
  cod_num_operacion STRING COMMENT 'Codigo numero operación',
  pct_pi_cli DECIMAL(30, 6) COMMENT 'probabilidad de incumplimiento del cliente',
  mnt_operacion DECIMAL(30, 6) COMMENT 'Monto Deuda Operacion',
  mnt_provision DECIMAL(30, 6) COMMENT 'Provisión estimada',
  cod_orig_deu STRING COMMENT 'Origen Deuda',
  cod_tipo_deu  STRING COMMENT 'Tipo Deuda',
  cod_tipo_garantia STRING COMMENT 'Tipo de Garantia',
  mnt_garantia DECIMAL(30, 6) COMMENT 'Monto Garantia',
  cod_calificacion  STRING COMMENT 'Clasificacion SBIF Cliente',
  pct_pi DECIMAL(30, 6) COMMENT 'probabilidad de incumplimiento del cliente',
  pct_pdi DECIMAL(30, 6) COMMENT 'perdida dado el incumplimiento de la operación',
  pct_pe DECIMAL(30, 6) COMMENT 'pérdida esperada' 
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_modelo_indv_leasing' """

# COMMAND ----------

sql_safe(create_tmp_modelo_indv_lea)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Tabla tmp_modelo_indv_leasing_resp

# COMMAND ----------

paso_tb_del_tmp_modelo_indv_lea_2  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_modelo_indv_leasing_resp """

# COMMAND ----------

sql_safe(paso_tb_del_tmp_modelo_indv_lea_2)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_modelo_indv_leasing_resp", True)

# COMMAND ----------

create_tmp_modelo_indv_lea2 = """CREATE TABLE """ + db_plat_tempX + """.tmp_modelo_indv_leasing_resp
(
  periodo_id INT COMMENT 'Periodo de ejecucion SSAAMM',
  rut_cliente INT COMMENT 'rut cliente',
  dv_cliente STRING COMMENT 'dígito verificador cliente',
  cod_num_operacion STRING COMMENT 'Codigo numero operación',
  pct_pi_cli DECIMAL(30, 6) COMMENT 'probabilidad de incumplimiento del cliente',
  mnt_operacion DECIMAL(30, 6) COMMENT 'Monto Deuda Operacion',
  mnt_provision DECIMAL(30, 6) COMMENT 'Provisión estimada',
  cod_orig_deu STRING COMMENT 'Origen Deuda',
  cod_tipo_deu  STRING COMMENT 'Tipo Deuda',
  cod_tipo_garantia STRING COMMENT 'Tipo de Garantia',
  mnt_garantia DECIMAL(30, 6) COMMENT 'Monto Garantia',
  cod_calificacion  STRING COMMENT 'Clasificacion SBIF Cliente',
  pct_pi DECIMAL(30, 6) COMMENT 'probabilidad de incumplimiento del cliente',
  pct_pdi DECIMAL(30, 6) COMMENT 'perdida dado el incumplimiento de la operación',
  pct_pe DECIMAL(30, 6) COMMENT 'pérdida esperada' 
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_modelo_indv_leasing_resp' """

# COMMAND ----------

sql_safe(create_tmp_modelo_indv_lea2)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Tabla tmp_d00_comercial_grupal

# COMMAND ----------

paso_tb_del_tmp_d00_comercial_grupal  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_d00_comercial_grupal"""

# COMMAND ----------

sql_safe(paso_tb_del_tmp_d00_comercial_grupal)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_d00_comercial_grupal", True)

# COMMAND ----------

paso_tb_crea_tmp_d00_comercial_grupal = """CREATE TABLE """ + db_plat_tempX + """.tmp_d00_comercial_grupal
(
  periodo_id INT COMMENT 'Periodo de ejecucion SSAAMM',
  rut_cliente INT COMMENT 'RUT del Cliente Titular',
  dv_cliente STRING COMMENT 'Digito verificador del cliente titular',
	cod_ope_original STRING COMMENT 'Operación Original (TRZ)',
  cod_num_operacion STRING COMMENT 'Número Operación',
  cod_tip_cart STRING COMMENT 'Tipo cartera (OPE_VIGENTE) ',
	cod_tipo_ope STRING COMMENT 'Tipo-subtipo del D00',
	des_producto_ope STRING COMMENT  'Descripción Tipo de Cartera',
  des_banca_ope STRING COMMENT 'Descripción operacion banca',
	cod_cartera_ope STRING COMMENT 'Subtipo cartera',
	mnt_deuda_ope	   DECIMAL(32,0) COMMENT 'Saldo total ifrs',
  num_dias_mora_ope INT COMMENT 'Dias de mora',
  ind_cdet STRING COMMENT 'Indicador cartera deterioro',
  fec_ingreso_deteriodo_ope DATE COMMENT 'Fecha ingreso de deterioro de la operación',
  cod_cctb STRING COMMENT 'Cuenta contable',
  cod_cta_ifrs STRING COMMENT 'Cuenta IFRS',
  fl_ope_reneg STRING COMMENT 'Flag de cartera renegociada',
  cod_tipo_activo INT COMMENT 'Tipo Activo Cartera',
	nombre_activo STRING COMMENT 'Nombre Activo Cartera',
	pct_pi DECIMAL(12,6) COMMENT 'probabilidad de incumplimiento del cliente',
  pct_pdi DECIMAL(12,6) COMMENT 'perdida dado el incumplimiento de la operación',
  pct_pe DECIMAL(12,6) COMMENT 'pérdida esperada',
	mnt_provision DECIMAL(32,0) COMMENT 'Prov Oficial',
	cod_tipo_cli STRING COMMENT 'Tipo Cliente GR/IND',
	mnt_exposicion DECIMAL(32,0) COMMENT 'Prov Oficial',
	factor_expo DECIMAL(12,6) COMMENT 'Factor de Exposición',
	des_tipo_gtia STRING COMMENT 'Tipo garantia',		
	mnt_garantia DECIMAL(32,0) COMMENT 'MONTO_GARANTIA' ,
	pct_ltv DECIMAL(12,6) COMMENT 'relación entre la deuda actual y el valor del bien al origen',
	pct_pi_metodo_interno DECIMAL(12,6) COMMENT 'PI metodo interno',
  pct_pdi_metodo_interno DECIMAL(12,6) COMMENT 'PDI metodo interno',
  pct_pe_metodo_interno DECIMAL(12,6) COMMENT 'PE metodo interno',
	mnt_prov_metodo_interno  DECIMAL(32,0) COMMENT 'Provisión de la Colocación',
	mnt_avalado DECIMAL(32,0) COMMENT 'MONTO_AVALADO'
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_d00_comercial_grupal' """

# COMMAND ----------

sql_safe(paso_tb_crea_tmp_d00_comercial_grupal)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Desarrollo Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 1: Obtencion Operaciones Leasing (Sumando Provisiones)

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de operaciones Duplicadas Leasing, Totalizando provisiones de tabla de Modelo Individual, tabla de salida de Notebook 003
# MAGIC
# MAGIC **Tabla de salida**: tmp_modelo_indv_leasing (Temporal) 
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: rut cliente
# MAGIC - **dv_cliente**: dígito verificador cliente
# MAGIC - **cod_num_operacion**: Codigo numero operación
# MAGIC - **pct_pi_cli**: probabilidad de incumplimiento del cliente
# MAGIC - **mnt_operacion**: Monto Deuda Operacion
# MAGIC - **mnt_provision**: Provisión estimada
# MAGIC - **cod_orig_deu**: Origen Deuda
# MAGIC - **cod_tipo_deu**: Tipo Deuda
# MAGIC - **cod_tipo_garantia**: Tipo de Garantia
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **cod_calificacion**: Clasificacion SBIF Cliente
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada

# COMMAND ----------

query_leasing_indv_tot_prov = """ Insert into """+db_plat_tempX+""".tmp_modelo_indv_leasing
select periodo_id
    ,rut_cliente	
    ,dv_cliente
    ,cod_num_operacion	
    ,pct_pi_cli	
    ,mnt_operacion
    ,SUM(mnt_provision)     AS mnt_provision
    ,cod_orig_deu	
    ,cod_tipo_deu	
    ,cod_tipo_garantia
    ,mnt_garantia	
    ,cod_calificacion	
    ,pct_pi	
    ,pct_pdi	
    ,pct_pe
from """+db_plat_tempX+""".tmp_modelo_indiv
where periodo_id = """+anomes+"""
group by periodo_id
    ,rut_cliente	
    ,dv_cliente
    ,cod_num_operacion	
    ,pct_pi_cli	
    ,mnt_operacion
    ,cod_orig_deu	
    ,cod_tipo_deu	
    ,cod_tipo_garantia
    ,mnt_garantia	
    ,cod_calificacion	
    ,pct_pi	
    ,pct_pdi	
    ,pct_pe
"""

# COMMAND ----------

sql_safe(query_leasing_indv_tot_prov)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 2: Obtencion Operaciones Leasing (Obtencion Maximo PI Cliente)

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de operaciones Duplicadas Leasing, Agrupando por PI de Cliente de tabla de salida en paso anterior
# MAGIC
# MAGIC **Tabla de salida**: tmp_modelo_indv_leasing_resp (Temporal) 
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: rut cliente
# MAGIC - **dv_cliente**: dígito verificador cliente
# MAGIC - **cod_num_operacion**: Codigo numero operación
# MAGIC - **pct_pi_cli**: probabilidad de incumplimiento del cliente
# MAGIC - **mnt_operacion**: Monto Deuda Operacion
# MAGIC - **mnt_provision**: Provisión estimada
# MAGIC - **cod_orig_deu**: Origen Deuda
# MAGIC - **cod_tipo_deu**: Tipo Deuda
# MAGIC - **cod_tipo_garantia**: Tipo de Garantia
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **cod_calificacion**: Clasificacion SBIF Cliente
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada

# COMMAND ----------

query_leasing_indv_mx_pi = """ Insert into """+db_plat_tempX+""".tmp_modelo_indv_leasing_resp
select periodo_id
    ,rut_cliente	
    ,dv_cliente
    ,cod_num_operacion	
    ,MAX(pct_pi_cli)        AS pct_pi_cli	
    ,mnt_operacion
    ,mnt_provision
    ,cod_orig_deu	
    ,cod_tipo_deu	
    ,cod_tipo_garantia
    ,mnt_garantia	
    ,cod_calificacion	
    ,pct_pi	
    ,pct_pdi	
    ,pct_pe
from   """+db_plat_tempX+""".tmp_modelo_indv_leasing 
where periodo_id =  """+anomes+"""
group by periodo_id
    ,rut_cliente	
    ,dv_cliente
    ,cod_num_operacion	
    ,mnt_operacion
    ,mnt_provision
    ,cod_orig_deu	
    ,cod_tipo_deu	
    ,cod_tipo_garantia
    ,mnt_garantia	
    ,cod_calificacion	
    ,pct_pi	
    ,pct_pdi	
    ,pct_pe """

# COMMAND ----------

sql_safe(query_leasing_indv_mx_pi)

# COMMAND ----------

qry_dlt_lea = """ delete from """+db_plat_tempX+""".tmp_modelo_indv_leasing where periodo_id = """+anomes+""" """

sql_safe(qry_dlt_lea)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 3: Obtencion Operaciones Leasing (Obtencion Totalizador Monto de la Operación)

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de operaciones Duplicadas Leasing, Totalizando Monto Operaciones de tabla de salida en paso anterior
# MAGIC
# MAGIC **Tabla de salida**: tmp_modelo_indv_leasing (Temporal) 
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: rut cliente
# MAGIC - **dv_cliente**: dígito verificador cliente
# MAGIC - **cod_num_operacion**: Codigo numero operación
# MAGIC - **pct_pi_cli**: probabilidad de incumplimiento del cliente
# MAGIC - **mnt_operacion**: Monto Deuda Operacion
# MAGIC - **mnt_provision**: Provisión estimada
# MAGIC - **cod_orig_deu**: Origen Deuda
# MAGIC - **cod_tipo_deu**: Tipo Deuda
# MAGIC - **cod_tipo_garantia**: Tipo de Garantia
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **cod_calificacion**: Clasificacion SBIF Cliente
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada

# COMMAND ----------

query_leasing_indv_tot_ope = """ Insert into """+db_plat_tempX+""".tmp_modelo_indv_leasing
select periodo_id
    ,rut_cliente	
    ,dv_cliente
    ,cod_num_operacion	
    ,pct_pi_cli	
    ,MAX(mnt_operacion)     AS mnt_operacion
    ,mnt_provision
    ,cod_orig_deu	
    ,cod_tipo_deu	
    ,cod_tipo_garantia
    ,mnt_garantia	
    ,cod_calificacion	
    ,pct_pi	
    ,pct_pdi	
    ,pct_pe
from  """+db_plat_tempX+""".tmp_modelo_indv_leasing_resp
where periodo_id = """+anomes+"""
group by periodo_id
    ,rut_cliente	
    ,dv_cliente
    ,cod_num_operacion	
    ,pct_pi_cli
    ,mnt_provision
    ,cod_orig_deu	
    ,cod_tipo_deu	
    ,cod_tipo_garantia
    ,mnt_garantia	
    ,cod_calificacion	
    ,pct_pi	
    ,pct_pdi	
    ,pct_pe """

# COMMAND ----------

sql_safe(query_leasing_indv_tot_ope)

# COMMAND ----------

qry_dlt_lea_resp = """ delete from """+db_plat_tempX+""".tmp_modelo_indv_leasing_resp where periodo_id = """+anomes+""" """

sql_safe(qry_dlt_lea_resp)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 4: Obtencion Operaciones Leasing (Obtencion Maximo Tipo Garantia)

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de operaciones Duplicadas Leasing, Agrupando por Tipo de Garantia de tabla de salida en paso anterior
# MAGIC
# MAGIC **Tabla de salida**: tmp_modelo_indv_leasing_resp (Temporal) 
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: rut cliente
# MAGIC - **dv_cliente**: dígito verificador cliente
# MAGIC - **cod_num_operacion**: Codigo numero operación
# MAGIC - **pct_pi_cli**: probabilidad de incumplimiento del cliente
# MAGIC - **mnt_operacion**: Monto Deuda Operacion
# MAGIC - **mnt_provision**: Provisión estimada
# MAGIC - **cod_orig_deu**: Origen Deuda
# MAGIC - **cod_tipo_deu**: Tipo Deuda
# MAGIC - **cod_tipo_garantia**: Tipo de Garantia
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **cod_calificacion**: Clasificacion SBIF Cliente
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada

# COMMAND ----------

query_leasing_indv_mx_tgar = """ Insert into """+db_plat_tempX+""".tmp_modelo_indv_leasing_resp
select periodo_id
    ,rut_cliente	
    ,dv_cliente
    ,cod_num_operacion	
    ,pct_pi_cli	
    ,mnt_operacion
    ,mnt_provision
    ,cod_orig_deu	
    ,cod_tipo_deu	
    ,MAX(cod_tipo_garantia) AS cod_tipo_garantia
    ,mnt_garantia	
    ,cod_calificacion	
    ,pct_pi	
    ,pct_pdi	
    ,pct_pe
from  """+db_plat_tempX+""".tmp_modelo_indv_leasing
where periodo_id = """+anomes+"""
group by periodo_id
    ,rut_cliente	
    ,dv_cliente
    ,cod_num_operacion	
    ,pct_pi_cli
    ,mnt_operacion
    ,mnt_provision
    ,cod_orig_deu	
    ,cod_tipo_deu	
    ,mnt_garantia	
    ,cod_calificacion	
    ,pct_pi	
    ,pct_pdi	
    ,pct_pe """

# COMMAND ----------

sql_safe(query_leasing_indv_mx_tgar)

# COMMAND ----------

qry_dlt_lea = """ delete from """+db_plat_tempX+""".tmp_modelo_indv_leasing where periodo_id = """+anomes+""" """

sql_safe(qry_dlt_lea)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 5: Obtencion Operaciones Leasing (Obtencion Totalizador Monto Garantia)

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de operaciones Duplicadas Leasing, Totalizando Monto Garantia de tabla de salida en paso anterior
# MAGIC
# MAGIC **Tabla de salida**: tmp_modelo_indv_leasing (Temporal) 
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: rut cliente
# MAGIC - **dv_cliente**: dígito verificador cliente
# MAGIC - **cod_num_operacion**: Codigo numero operación
# MAGIC - **pct_pi_cli**: probabilidad de incumplimiento del cliente
# MAGIC - **mnt_operacion**: Monto Deuda Operacion
# MAGIC - **mnt_provision**: Provisión estimada
# MAGIC - **cod_orig_deu**: Origen Deuda
# MAGIC - **cod_tipo_deu**: Tipo Deuda
# MAGIC - **cod_tipo_garantia**: Tipo de Garantia
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **cod_calificacion**: Clasificacion SBIF Cliente
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada

# COMMAND ----------

query_leasing_indv_tot_gtia = """ Insert into """+db_plat_tempX+""".tmp_modelo_indv_leasing
select periodo_id
    ,rut_cliente	
    ,dv_cliente
    ,cod_num_operacion	
    ,pct_pi_cli	
    ,mnt_operacion
    ,mnt_provision
    ,cod_orig_deu	
    ,cod_tipo_deu	
    ,cod_tipo_garantia
    ,MAX(mnt_garantia)      AS mnt_garantia		
    ,cod_calificacion	
    ,pct_pi	
    ,pct_pdi	
    ,pct_pe
from  """+db_plat_tempX+""".tmp_modelo_indv_leasing_resp
where periodo_id = """+anomes+"""
group by periodo_id
    ,rut_cliente	
    ,dv_cliente
    ,cod_num_operacion	
    ,pct_pi_cli
    ,mnt_operacion
    ,mnt_provision
    ,cod_orig_deu	
    ,cod_tipo_deu	
    ,cod_tipo_garantia
    --,mnt_garantia	
    ,cod_calificacion	
    ,pct_pi	
    ,pct_pdi	
    ,pct_pe """

# COMMAND ----------

sql_safe(query_leasing_indv_tot_gtia)

# COMMAND ----------

qry_dlt_lea_resp = """ delete from """+db_plat_tempX+""".tmp_modelo_indv_leasing_resp where periodo_id = """+anomes+""" """

sql_safe(qry_dlt_lea_resp)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 6:  Obtencion Operaciones Leasing (Obtencion Maximo PI )

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de operaciones Duplicadas Leasing, Agrupando por PI de tabla de salida en paso anterior
# MAGIC
# MAGIC **Tabla de salida**: tmp_modelo_indv_leasing_resp (Temporal) 
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: rut cliente
# MAGIC - **dv_cliente**: dígito verificador cliente
# MAGIC - **cod_num_operacion**: Codigo numero operación
# MAGIC - **pct_pi_cli**: probabilidad de incumplimiento del cliente
# MAGIC - **mnt_operacion**: Monto Deuda Operacion
# MAGIC - **mnt_provision**: Provisión estimada
# MAGIC - **cod_orig_deu**: Origen Deuda
# MAGIC - **cod_tipo_deu**: Tipo Deuda
# MAGIC - **cod_tipo_garantia**: Tipo de Garantia
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **cod_calificacion**: Clasificacion SBIF Cliente
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada

# COMMAND ----------

query_leasing_indv_max_pi = """ Insert into """+db_plat_tempX+""".tmp_modelo_indv_leasing_resp
select periodo_id
    ,rut_cliente	
    ,dv_cliente
    ,cod_num_operacion	
    ,pct_pi_cli	
    ,mnt_operacion
    ,mnt_provision
    ,cod_orig_deu	
    ,cod_tipo_deu	
    ,cod_tipo_garantia
    ,mnt_garantia		
    ,cod_calificacion	
    ,max(pct_pi) pct_pi
    ,pct_pdi	
    ,pct_pe
from  """+db_plat_tempX+""".tmp_modelo_indv_leasing
where periodo_id = """+anomes+"""
group by periodo_id
    ,rut_cliente	
    ,dv_cliente
    ,cod_num_operacion	
    ,pct_pi_cli
    ,mnt_operacion
    ,mnt_provision
    ,cod_orig_deu	
    ,cod_tipo_deu	
    ,cod_tipo_garantia
    ,mnt_garantia	
    ,cod_calificacion	
    --,pct_pi	
    ,pct_pdi	
    ,pct_pe
"""

# COMMAND ----------

sql_safe(query_leasing_indv_max_pi)

# COMMAND ----------

qry_dlt_lea = """ delete from """+db_plat_tempX+""".tmp_modelo_indv_leasing where periodo_id = """+anomes+""" """

sql_safe(qry_dlt_lea)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 7: Obtencion Operaciones Leasing (Obtencion Maximo PDI )

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de operaciones Duplicadas Leasing, Agrupando por PDI de tabla de salida en paso anterior
# MAGIC
# MAGIC **Tabla de salida**: tmp_modelo_indv_leasing (Temporal) 
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: rut cliente
# MAGIC - **dv_cliente**: dígito verificador cliente
# MAGIC - **cod_num_operacion**: Codigo numero operación
# MAGIC - **pct_pi_cli**: probabilidad de incumplimiento del cliente
# MAGIC - **mnt_operacion**: Monto Deuda Operacion
# MAGIC - **mnt_provision**: Provisión estimada
# MAGIC - **cod_orig_deu**: Origen Deuda
# MAGIC - **cod_tipo_deu**: Tipo Deuda
# MAGIC - **cod_tipo_garantia**: Tipo de Garantia
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **cod_calificacion**: Clasificacion SBIF Cliente
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada

# COMMAND ----------

query_leasing_indv_max_pdi = """ Insert into """+db_plat_tempX+""".tmp_modelo_indv_leasing
select periodo_id
    ,rut_cliente	
    ,dv_cliente
    ,cod_num_operacion	
    ,pct_pi_cli	
    ,mnt_operacion
    ,mnt_provision
    ,cod_orig_deu	
    ,cod_tipo_deu	
    ,cod_tipo_garantia
    ,mnt_garantia		
    ,cod_calificacion	
    ,pct_pi	
    ,max(pct_pdi) pct_pdi
    ,pct_pe
from """+db_plat_tempX+""".tmp_modelo_indv_leasing_resp
where periodo_id = """+anomes+"""
group by periodo_id
    ,rut_cliente	
    ,dv_cliente
    ,cod_num_operacion	
    ,pct_pi_cli	
    ,mnt_operacion
    ,mnt_provision
    ,cod_orig_deu	
    ,cod_tipo_deu	
    ,cod_tipo_garantia
    ,mnt_garantia		
    ,cod_calificacion	
    ,pct_pi	
    --,pct_pdi	
    ,pct_pe
"""

# COMMAND ----------

sql_safe(query_leasing_indv_max_pdi)

# COMMAND ----------

qry_dlt_lea_resp = """ delete from """+db_plat_tempX+""".tmp_modelo_indv_leasing_resp where periodo_id = """+anomes+""" """

sql_safe(qry_dlt_lea_resp)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 8: Obtencion Operaciones Leasing (Obtencion Maxima PE )

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de operaciones Duplicadas Leasing, Agrupando por PE de tabla de salida en paso anterior
# MAGIC
# MAGIC **Tabla de salida**: tmp_modelo_indv_leasing_resp (Temporal) 
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: rut cliente
# MAGIC - **dv_cliente**: dígito verificador cliente
# MAGIC - **cod_num_operacion**: Codigo numero operación
# MAGIC - **pct_pi_cli**: probabilidad de incumplimiento del cliente
# MAGIC - **mnt_operacion**: Monto Deuda Operacion
# MAGIC - **mnt_provision**: Provisión estimada
# MAGIC - **cod_orig_deu**: Origen Deuda
# MAGIC - **cod_tipo_deu**: Tipo Deuda
# MAGIC - **cod_tipo_garantia**: Tipo de Garantia
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **cod_calificacion**: Clasificacion SBIF Cliente
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada

# COMMAND ----------

query_leasing_indv_max_pe = """ Insert into """+db_plat_tempX+""".tmp_modelo_indv_leasing_resp
select periodo_id
    ,rut_cliente	
    ,dv_cliente
    ,cod_num_operacion	
    ,pct_pi_cli	
    ,mnt_operacion
    ,mnt_provision
    ,cod_orig_deu	
    ,cod_tipo_deu	
    ,cod_tipo_garantia
    ,mnt_garantia		
    ,cod_calificacion	
    ,pct_pi	
    ,pct_pdi
    ,max(pct_pe) pct_pe
from """+db_plat_tempX+""".tmp_modelo_indv_leasing
where periodo_id = """+anomes+"""
group by periodo_id
    ,rut_cliente	
    ,dv_cliente
    ,cod_num_operacion	
    ,pct_pi_cli	
    ,mnt_operacion
    ,mnt_provision
    ,cod_orig_deu	
    ,cod_tipo_deu	
    ,cod_tipo_garantia
    ,mnt_garantia		
    ,cod_calificacion	
    ,pct_pi	
    ,pct_pdi	
    --,pct_pe
  """

# COMMAND ----------

sql_safe(query_leasing_indv_max_pe)

# COMMAND ----------

qry_dlt_lea = """ delete from """+db_plat_tempX+""".tmp_modelo_indv_leasing where periodo_id = """+anomes+""" """

sql_safe(qry_dlt_lea)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 9: Obtencion Cuentas Contables mal catalogadas de Comercial Grupal

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de operaciones desde d00 cruzando con operaciones comerciales grupales (Tipo Activo = 13), estas operaciones se generan en Notebook 001 y 003
# MAGIC
# MAGIC **Tabla de salida**: tmp_d00_comercial_grupal (Temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera (OPE_VIGENTE)
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Subtipo cartera
# MAGIC - **mto_deuda_ope**: Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **nombre_activo**: Nombre Activo Cartera
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada
# MAGIC - **mto_provision**: Prov Oficial
# MAGIC - **cod_tipo_cli**: Calificacion Modelo Individual
# MAGIC - **mto_exposicion**: Prov Oficial
# MAGIC - **factor_expo**: pct_fcc
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: MONTO_GARANTIA' ,
# MAGIC - **pct_ltv**: relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado

# COMMAND ----------

query_d00_grupal = """ INSERT INTO """ + db_plat_tempX + """.tmp_d00_comercial_grupal
select d00.periodo_id
	,d00.rut_cliente
  ,d00.dv_cliente
	,d00.cod_ope_original
  ,d00.cod_num_operacion
  ,d00.cod_tip_cart 
	,d00.cod_tipo_ope
	,d00.des_producto_ope
  ,d00.des_banca_ope
	,d00.cod_cartera_ope
	,d00.mto_deuda_ope
  ,d00.num_dias_mora_ope
  ,d00.ind_cdet
  ,d00.fec_ingreso_deteriodo_ope
  ,d00.cod_cctb
	,'' as cod_cta_ifrs
  ,d00.fl_ope_reneg 
  ,13 as cod_tipo_activo
	,'LEASING COMERCIAL' as nombre_activo 
	,b.pct_pi
  ,b.pct_pdi
  ,b.pct_pe
	,b.mnt_prov_oficial as mnt_provision
	,case when b.periodo_id is null then null else 'GR' end  as cod_tipo_cli
	,0 as mto_exposicion
	,0 as factor_expo
	,b.des_tipo_gtia as TIPO_GARANTIA				
	,b.mnt_garantia as MONTO_GARANTIA
	,b.pct_ltv as PTVG 
	,b.pct_pi_metodo_interno as pct_pi_metodo_interno
  ,b.pct_pdi_metodo_interno as  pct_pdi_metodo_interno
  ,b.pct_pe_metodo_interno  as pct_pe_metodo_interno
	,b.mnt_prov_int as mto_PROV_METODO_INTERNO 
	,coalesce(b.mnt_avalado,0) as mto_avalado
from """+db_plat_tempX+""".tmp_d00_deuda_act_ctg d00
	LEFT JOIN """+db_plat_tempX+""".tmp_modelo_com_gr b
			ON	D00.Periodo_Id = b.periodo_id
        and trim(d00.cod_num_operacion) = trim(b.cod_num_operacion)
        and trim(d00.cod_cartera_ope) = trim(b.cod_stp_cart)			
where d00.periodo_id = """+anomes+"""
  and trim(d00.cod_cartera_ope) = 'ACT'
  and trim(d00.des_banca_ope) = 'LEASING'  
  and trim(d00.des_producto_ope) ='COMERCIAL'
"""

# COMMAND ----------

sql_safe(query_d00_grupal)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 10: Eliminación Operaciones Leasing Comercial (Grupal e Individual) para fecha de Proceso

# COMMAND ----------

query_delete_lea_com = """DELETE FROM """ + db_platinumX + """.base_archivos_normativos WHERE periodo_id = """+anomes+""" and cod_tabla_89 = 13  """

# COMMAND ----------

sql_safe(query_delete_lea_com)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 11: Insert Operaciones Leasing Comercial

# COMMAND ----------

# MAGIC %md
# MAGIC se insertan Operaciones Leasing Comercial, Grupales e Individuales (Tipo Activo = 13)
# MAGIC
# MAGIC **Tabla de salida**: base_normativa_v3_ft 
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera (OPE_VIGENTE)
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Subtipo cartera
# MAGIC - **mto_deuda_ope**: Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **nombre_activo**: Nombre Activo Cartera
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada
# MAGIC - **mto_provision**: Prov Oficial
# MAGIC - **cod_tipo_cli**: Tipo Cliente GR/IND
# MAGIC - **mto_exposicion**: Prov Oficial
# MAGIC - **factor_expo**: pct_fcc
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: MONTO_GARANTIA' ,
# MAGIC - **pct_ltv**: relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado

# COMMAND ----------

ult_dia_mes = ultimo_dia_habil(db_slv_ParametricasX,anomes)

ult_dia = ult_dia_mes.toPandas()
fec_proceso  = ult_dia.iloc[0]["periodo_id"]

ano_proceso = str(fec_proceso)[:4]
mes_proceso = str(fec_proceso)[4:][:2]
dia_proceso = str(fec_proceso)[6:][:2]
date_proceso = str(ano_proceso+'-'+mes_proceso+'-'+dia_proceso)

print(date_proceso)

# COMMAND ----------

query_ins_col_lea_com = """ insert into """ + db_platinumX + """.base_archivos_normativos
select d00.periodo_id
	,d00.rut_cliente
  ,d00.dv_cliente
	,d00.cod_ope_original
  ,CASE WHEN substring(d00.cod_tipo_ope, 1,3)='LEA'THEN 'LEA'|| SUBSTRING(TRIM(d00.cod_num_operacion),4,15) ELSE TRIM(d00.cod_num_operacion) END  as cod_num_operacion  
  ,d00.cod_tip_cart 
	,d00.cod_tipo_ope
	,d00.des_producto_ope
  ,d00.des_banca_ope
	,d00.cod_cartera_ope
	,d00.mnt_deuda_ope
  ,d00.num_dias_mora_ope
  ,d00.ind_cdet
  ,d00.fec_ingreso_deteriodo_ope
  ,d00.cod_cctb
  ,d00.fl_ope_reneg 
  ,-998                              as cod_tabla_34
  ,13 as cod_tipo_activo
	,'LEASING COMERCIAL' as nombre_activo 
	,coalesce(d00.pct_pi, b.pct_pi)
  ,coalesce(d00.pct_pdi, b.pct_pi)
  ,coalesce(d00.pct_pe, b.pct_pi)
	,case when (d00.mnt_provision is null) then b.mnt_provision else d00.mnt_provision end as mnt_provision
 --,coalesce(d00.mnt_provision, b.mnt_provision) as mnt_provision
	,coalesce(d00.cod_tipo_cli, b.cod_calificacion)  as cod_tipo_cli
	,d00.mnt_exposicion
	,d00.factor_expo
	,coalesce(d00.des_tipo_gtia, b.cod_tipo_garantia) as TIPO_GARANTIA				
	,coalesce(d00.mnt_garantia, b.mnt_garantia) as MONTO_GARANTIA
	,coalesce(d00.pct_ltv, 0) as PTVG 
	,coalesce(d00.pct_pi_metodo_interno, 0) as pct_pi_metodo_interno
  ,coalesce(d00.pct_pdi_metodo_interno, 0) as  pct_pdi_metodo_interno
  ,coalesce(d00.pct_pe_metodo_interno, 0)  as pct_pe_metodo_interno
	,coalesce(d00.mnt_prov_metodo_interno, 0) as mto_PROV_METODO_INTERNO 
	,coalesce(d00.mnt_avalado, 0) as mto_avalado
  ,'"""+str(date_proceso)+"""'
from """+db_plat_tempX+""".tmp_d00_comercial_grupal d00	 		
	LEFT JOIN """+db_plat_tempX+""".tmp_modelo_indv_leasing_resp b
			ON	d00.Periodo_Id = b.periodo_id
      AND trim(d00.cod_num_operacion) = trim(b.cod_num_operacion)
			AND trim(b.cod_tipo_deu) = 'NCONT'
			AND b.mnt_provision <> 0
   		AND d00.cod_cartera_ope = (CASE WHEN trim(b.cod_tipo_deu)='NCONT' THEN 'ACT' ELSE 'CTG' END) 
where d00.periodo_id = """+anomes+""" """

# COMMAND ----------

sql_safe(query_ins_col_lea_com)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Conteo de registros insertados en tabla de salida

# COMMAND ----------

registros = spark.sql(""" select count(1) as cant_registros from """ + db_platinumX + """.base_archivos_normativos  where periodo_id = """+anomes+""" and cod_tabla_89 in (13) """)

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Insertaron " + str(cantidad) +" Registros en la tabla temporal base_archivos_normativos ")

# COMMAND ----------

# MAGIC %md
# MAGIC # Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")