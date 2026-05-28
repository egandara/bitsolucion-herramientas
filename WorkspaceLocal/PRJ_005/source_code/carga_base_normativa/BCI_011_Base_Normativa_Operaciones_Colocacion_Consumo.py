# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_011_Base_Normativa_Operaciones_Colocacion_Consumo

# COMMAND ----------

# MAGIC %md
# MAGIC ## Información del Notebook

# COMMAND ----------

# MAGIC %md
# MAGIC ### Encabezado
# MAGIC **************************************************************************
# MAGIC * Nombre: BCI_011_Base_Normativa_Operaciones_Colocacion_Consumo.ipynb
# MAGIC * Ruta: 
# MAGIC * Autor:  Jonathan Araya R.
# MAGIC * Ing. SW BCI: <alguien.de.ti@bci.cl> - Marcelo Lizama <marcelo.lizama@bci.cl> - Ivan Jara <ivan.jara@bci.cl>
# MAGIC * Fecha: 22/12/2025
# MAGIC * Descripción: Notebook encargado de registrar información de Operaciones de Colocaciones Consumo
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
# MAGIC * tmp_d00_deuda_act_ctg
# MAGIC * tmp_modelo_consumo
# MAGIC * dsr_slv_Parametricas_db.tbu
# MAGIC ***************************************************************************
# MAGIC #### Tabla Salida Temporal: 
# MAGIC * base_archivos_normativos (Activo 31, 32, 33, 34 Tabla 89 Manual CMF)

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
# MAGIC ## Carga de funciones

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
#valida_param_vacio(db_platinumX,'db_platinumX')
valida_param_vacio(db_plat_tempX,'db_plat_tempX')
valida_param_vacio(db_RiesgoCredX,'db_RiesgoCredX')
valida_param_vacio(db_location_plat_tempX,'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Base de Datos

# COMMAND ----------

#valida_bd(db_platinumX, 'db_platinumX')
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
# MAGIC ### Tabla tmp_colocaciones_consumo

# COMMAND ----------

paso_tb_del_tmp_cta_ctble_consumo  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_colocaciones_consumo """

# COMMAND ----------

sql_safe(paso_tb_del_tmp_cta_ctble_consumo)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_colocaciones_consumo", True)

# COMMAND ----------

paso_tb_crea_tmp_cta_ctble_consumo = """CREATE TABLE """ + db_plat_tempX + """.tmp_colocaciones_consumo
(
  periodo_id INT COMMENT 'Periodo de ejecucion SSAAMM',
  rut_cliente INT COMMENT 'RUT del Cliente Titular',
  dv_cliente STRING COMMENT 'Digito verificador del cliente titular',
	cod_ope_original STRING COMMENT 'Operación Original (TRZ)',
  cod_num_operacion STRING COMMENT 'Número Operación',
  cod_tip_cart STRING COMMENT 'Tipo cartera ',
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
	factor_expo DECIMAL(12,6) COMMENT 'PI',
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
LOCATION '"""+ db_location_plat_tempX +"""tmp_colocaciones_consumo' """

# COMMAND ----------

sql_safe(paso_tb_crea_tmp_cta_ctble_consumo)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tabla tmp_colocaciones_consumo_2

# COMMAND ----------

paso_tb_del_tmp_col_consumo2  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_colocaciones_consumo_2"""

# COMMAND ----------

sql_safe(paso_tb_del_tmp_col_consumo2)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_colocaciones_consumo_2", True)

# COMMAND ----------

paso_tb_crea_tmp_col_consumo2 = """CREATE TABLE """ + db_plat_tempX + """.tmp_colocaciones_consumo_2
(
  periodo_id INT COMMENT 'Periodo de ejecucion SSAAMM',
  rut_cliente INT COMMENT 'RUT del Cliente Titular',
  dv_cliente STRING COMMENT 'Digito verificador del cliente titular',
	cod_ope_original STRING COMMENT 'Operación Original (TRZ)',
  cod_num_operacion STRING COMMENT 'Número Operación',
  cod_tip_cart STRING COMMENT 'Tipo cartera ',
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
	factor_expo DECIMAL(12,6) COMMENT 'PI',
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
LOCATION '"""+ db_location_plat_tempX +"""tmp_colocaciones_consumo_2' """

# COMMAND ----------

sql_safe(paso_tb_crea_tmp_col_consumo2)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tabla tmp_colocaciones_consumo_3

# COMMAND ----------

paso_tb_del_tmp_col_consumo3  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_colocaciones_consumo_3"""

# COMMAND ----------

sql_safe(paso_tb_del_tmp_col_consumo3)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_colocaciones_consumo_3", True)

# COMMAND ----------

paso_tb_crea_tmp_col_consumo3 = """CREATE TABLE """ + db_plat_tempX + """.tmp_colocaciones_consumo_3
(
  periodo_id INT COMMENT 'Periodo de ejecucion SSAAMM',
  rut_cliente INT COMMENT 'RUT del Cliente Titular',
  dv_cliente STRING COMMENT 'Digito verificador del cliente titular',
	cod_ope_original STRING COMMENT 'Operación Original (TRZ)',
  cod_num_operacion STRING COMMENT 'Número Operación',
  cod_tip_cart STRING COMMENT 'Tipo cartera ',
	cod_tipo_ope STRING COMMENT 'Tipo-subtipo del D00',
	des_producto_ope STRING COMMENT  'Descripción Tipo de Cartera',
  des_banca_ope STRING COMMENT 'Descripción operacion banca',
	cod_cartera_ope STRING COMMENT 'Subtipo cartera',
	mnt_deuda_ope	   DECIMAL(32,0) COMMENT 'Saldo total ifrs',
  num_dias_mora_ope INT COMMENT 'Dias de mora',
  ind_cdet STRING COMMENT 'Indicador cartera deterioro',
  fec_ingreso_deteriodo_ope DATE COMMENT 'Fecha ingreso de deterioro de la operación',
  cod_cctb STRING COMMENT 'Cuenta contable',
  fl_ope_reneg STRING COMMENT 'Flag de cartera renegociada',
  cod_tipo_activo INT COMMENT 'Tipo Activo Cartera',
	nombre_activo STRING COMMENT 'Nombre Activo Cartera',
	pct_pi DECIMAL(12,6) COMMENT 'probabilidad de incumplimiento del cliente',
  pct_pdi DECIMAL(12,6) COMMENT 'perdida dado el incumplimiento de la operación',
  pct_pe DECIMAL(12,6) COMMENT 'pérdida esperada',
	mnt_provision DECIMAL(32,0) COMMENT 'Prov Oficial',
	cod_tipo_cli STRING COMMENT 'Tipo Cliente GR/IND',
	mnt_exposicion DECIMAL(32,0) COMMENT 'Prov Oficial',
	factor_expo DECIMAL(12,6) COMMENT 'PI',
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
LOCATION '"""+ db_location_plat_tempX +"""tmp_colocaciones_consumo_3' """

# COMMAND ----------

sql_safe(paso_tb_crea_tmp_col_consumo3)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Desarrollo Logica

# COMMAND ----------

diaHabil = spark.sql("""select max(fecha_informada) as periodo_id  FROM  """+ db_slv_ParametricasX +""".TBU  
where substring(fecha_informada, 1, 6) = """+ anomes +"""  """)

ultimoDiaHabil_tbu = diaHabil.toPandas()
dia_tbu = ultimoDiaHabil_tbu.iloc[0]["periodo_id"]
 
print(dia_tbu)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 1: Obtencion Colocaciones de Consumo (31)

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de operaciones de consumo utilizando tablas tmp_d00_deuda_act_ctg (Salida de Notebook 1)y tabla TBU para identificar cuenta Ifrs
# MAGIC
# MAGIC **Colocaciones de consumo**
# MAGIC - **148000100**:	Créditos de consumo en cuotas
# MAGIC - **148000902**:	Cuentas por cobrar a deudores de consumo
# MAGIC
# MAGIC **Tabla de salida**: tmp_prestamo_hip_vivienda(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera 
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Subtipo cartera
# MAGIC - **mto_deuda_ope**: Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cta_ifrs**: Cuenta IFRS
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **nombre_activo**: Nombre Activo Cartera
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada
# MAGIC - **mto_provision**: Prov Oficial
# MAGIC - **cod_tipo_cli**: Tipo Cliente GR/IND
# MAGIC - **mto_exposicion**: Prov Oficial
# MAGIC - **factor_expo**: PI
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **pct_ltv**:relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado

# COMMAND ----------

query_ins_ope_cons = """ insert into """+db_plat_tempX+""".tmp_colocaciones_consumo  
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
	,trim(ccc.cta_cmf) as cod_cta_ifrs
  ,d00.fl_ope_reneg 
	,31 as cod_tipo_activo
	,'COLOCACIONES CONSUMO' as nombre_activo 
	,0 as pct_pi
	,0 as pct_pdi
	,0 as pct_pe
	,0 as mto_provision
	,'GR' as cod_tipo_cli
	,0 as mto_exposicion
	,0.0 as factor_expo
	,'No_Tiene' as TIPO_GARANTIA				
	,0.0 as MONTO_GARANTIA
	,0 as PTVG 
	,0 as pct_pi_metodo_interno	
	,0 as pct_pdi_metodo_interno	
	,0 as pct_pe_metodo_interno	
	,0 as mto_PROV_METODO_INTERNO 
	,0 as mto_avalado
from """+db_plat_tempX+""".tmp_d00_deuda_act_ctg d00
	inner join """+db_slv_ParametricasX+""".TBU ccc
		on trim(d00.cod_cctb) = trim(ccc.cta_ctable) 
  	and d00.periodo_id = substring(ccc.fecha_informada, 1, 6)
where d00.periodo_id =  """+anomes+"""
	and trim(d00.cod_cartera_ope) = 'ACT'
 	and trim(ccc.cta_cmf) IN ('148000902','148000100')
  and ccc.fecha_informada = """+str(dia_tbu)+"""
  and trim(d00.fl_ope_reneg)  <> 'C' --(Reneg Castigo) 
  """

# COMMAND ----------

sql_safe(query_ins_ope_cons)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 2: Eliminación Colocaciones Consumo (31), tarjetas de crédito (32), leasing (33) y líneas de crédito (34) para fecha de Proceso

# COMMAND ----------

query_delete_consumo = """DELETE FROM """ + db_platinumX + """.base_archivos_normativos WHERE periodo_id = """+anomes+""" and cod_tabla_89 in (31, 32, 33, 34) """

# COMMAND ----------

sql_safe(query_delete_consumo)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 3: Actualizacion de Valores para modelo Interno de Consumo (31)

# COMMAND ----------

# MAGIC %md
# MAGIC Se insertan operaciones de Consumo utilizando tablas tmp_colocaciones_consumo generada en Paso 1 y tmp_modelo_consumo, Tabla de Salida de Notebook 005 (Operaciones Modelo interno) respectivamente
# MAGIC
# MAGIC **Tabla de salida**: tmp_colocaciones_consumo_2 (Temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera 
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Subtipo cartera
# MAGIC - **mto_deuda_ope**: Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cta_ifrs**: Cuenta IFRS
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **nombre_activo**: Nombre Activo Cartera
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada
# MAGIC - **mto_provision**: Prov Oficial
# MAGIC - **cod_tipo_cli**: Tipo Cliente GR/IND
# MAGIC - **mto_exposicion**: Prov Oficial
# MAGIC - **factor_expo**: Porcentaje Factor Exposicion
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **pct_ltv**: relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado
# MAGIC - **fec_proceso**: Fecha Cierre

# COMMAND ----------

query_ins_ope_cons2 = """ insert into """+db_plat_tempX+""".tmp_colocaciones_consumo_2  
select a.periodo_id
	,a.rut_cliente
  ,a.dv_cliente
	,a.cod_ope_original
  ,a.cod_num_operacion
  ,a.cod_tip_cart 
	,a.cod_tipo_ope
	,a.des_producto_ope
  ,a.des_banca_ope
	,a.cod_cartera_ope
	,a.mnt_deuda_ope
  ,a.num_dias_mora_ope
  ,a.ind_cdet
  ,a.fec_ingreso_deteriodo_ope
  ,a.cod_cctb
  ,a.cod_cta_ifrs
  ,a.fl_ope_reneg 
	,a.cod_tipo_activo
	,a.nombre_activo 
	,b.pct_pi
  ,b.pct_pdi
  ,b.pct_pe
	,a.mnt_provision as mto_provision
	,a.cod_tipo_cli
	,a.mnt_exposicion
	,a.factor_expo
	,a.des_tipo_gtia as des_tipo_gtia				
	,a.mnt_garantia as mnt_garantia
	,a.pct_ltv as pct_ltv 
	,b.pct_pi as pct_pi_metodo_interno
  ,b.pct_pdi as  pct_pdi_metodo_interno
  ,b.pct_pe  as pct_pe_metodo_interno
	,b.mnt_prov_totalactiva as mto_PROV_METODO_INTERNO 
	,a.mnt_avalado
from """+db_plat_tempX+""".tmp_colocaciones_consumo a
left join """+db_plat_tempX+""".tmp_modelo_consumo b
  on a.periodo_id = b.periodo_id
  and trim(a.cod_cartera_ope) = 'ACT'
  and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
  and trim(b.cod_modelo) = 'INT'
where a.periodo_id = """+anomes+""" 
	and a.cod_tipo_activo = 31 """

# COMMAND ----------

sql_safe(query_ins_ope_cons2)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 4: Actualizacion Valores Modelo EstandarColocaciones Consumo (31) 

# COMMAND ----------

# MAGIC %md
# MAGIC Se insertan operaciones de Consumo utilizando tablas tmp_colocaciones_consumo generada en Paso 1 y tmp_modelo_consumo, Tabla de Salida de Notebook 005 (Operaciones Modelo Estandar) respectivamente
# MAGIC
# MAGIC **Tabla de salida**: tmp_colocaciones_consumo_3 (Temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera 
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
# MAGIC - **factor_expo**: Porcentaje Factor Exposicion
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **pct_ltv**: relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado

# COMMAND ----------

query_ins_ope_cons3 = """ insert into """+db_plat_tempX+""".tmp_colocaciones_consumo_3
select a.periodo_id
	,a.rut_cliente
  ,a.dv_cliente
	,a.cod_ope_original
  ,a.cod_num_operacion
  ,a.cod_tip_cart 
	,a.cod_tipo_ope
	,a.des_producto_ope
  ,a.des_banca_ope
	,a.cod_cartera_ope
	,a.mnt_deuda_ope
  ,a.num_dias_mora_ope
  ,a.ind_cdet
  ,a.fec_ingreso_deteriodo_ope
  ,a.cod_cctb
  ,a.fl_ope_reneg 
	,a.cod_tipo_activo
	,a.nombre_activo 
	,b.pct_pi
  ,b.pct_pdi
  ,b.pct_pe
	,b.mnt_prov_totalactiva as mto_provision
	,a.cod_tipo_cli
	,a.mnt_exposicion
	,a.factor_expo
	,a.des_tipo_gtia as des_tipo_gtia				
	,a.mnt_garantia as mnt_garantia
	,a.pct_ltv as pct_ltv 
	,a.pct_pi as pct_pi_metodo_interno
  ,a.pct_pdi as  pct_pdi_metodo_interno
  ,a.pct_pe  as pct_pe_metodo_interno
	,a.mnt_prov_metodo_interno as mto_PROV_METODO_INTERNO 
	,a.mnt_avalado
from """+db_plat_tempX+""".tmp_colocaciones_consumo_2  a
left join """+db_plat_tempX+""".tmp_modelo_consumo b
  on a.periodo_id = b.periodo_id
  and trim(a.cod_cartera_ope) = 'ACT'
  and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
  and trim(b.cod_modelo) = 'STD'
where a.periodo_id = """+anomes+"""
  and a.cod_tipo_activo = 31 
"""

# COMMAND ----------

sql_safe(query_ins_ope_cons3)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 5: Obtencion Colocaciones Deudores por tarjetas de crédito (32)

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de operaciones de deudores por Tarjeta de Credito utilizando tablas tmp_d00_deuda_act_ctg (Salida de Notebook 1)y tabla TBU para identificar cuenta Ifrs
# MAGIC
# MAGIC **Deudores por tarjetas de crédito**
# MAGIC - **148000301**:	Créditos por tarjetas de crédito
# MAGIC - **148000302**:	Utilizaciones de tarjetas de crédito por cobrar
# MAGIC
# MAGIC
# MAGIC **Tabla de salida**: tmp_colocaciones_consumo(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera **: ,
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Subtipo cartera
# MAGIC - **mto_deuda_ope**: Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cta_ifrs**: Cuenta IFRS
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **nombre_activo**: Nombre Activo Cartera
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada
# MAGIC - **mto_provision**: Prov Oficial
# MAGIC - **cod_tipo_cli**: Tipo Cliente GR/IND
# MAGIC - **mto_exposicion**: Prov Oficial
# MAGIC - **factor_expo**: Porcentaje Factor Exposicion
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **pct_ltv**: relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado

# COMMAND ----------

query_ins_ope_tc = """ insert into """+db_plat_tempX+""".tmp_colocaciones_consumo  
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
	,trim(ccc.cta_cmf) as cod_cta_ifrs
  ,d00.fl_ope_reneg 
	,32 as cod_tipo_activo
	,'TARJETAS DE CRÉDITO' as nombre_activo 
	,0 as pct_pi
	,0 as pct_pdi
	,0 as pct_pe
	,0 as mto_provision
	,'GR' as cod_tipo_cli
	,0 as mto_exposicion
	,0.0 as factor_expo
	,'No_Tiene' as TIPO_GARANTIA				
	,0.0 as MONTO_GARANTIA
	,0 as PTVG 
	,0 as pct_pi_metodo_interno	
	,0 as pct_pdi_metodo_interno	
	,0 as pct_pe_metodo_interno	
	,0 as mto_PROV_METODO_INTERNO 
	,0 as mto_avalado
from """+db_plat_tempX+""".tmp_d00_deuda_act_ctg d00
	inner join """+db_slv_ParametricasX+""".TBU ccc
		on trim(d00.cod_cctb) = trim(ccc.cta_ctable) 
  	and d00.periodo_id = substring(ccc.fecha_informada, 1, 6)
where d00.periodo_id =  """+anomes+"""
	and trim(d00.cod_cartera_ope) = 'ACT'
  and ccc.fecha_informada = """+str(dia_tbu)+"""
 	--and substring(trim(ccc.cta_cmf), 1,7) in ('1454004' , '1480003')
	--and trim(ccc.cta_cmf) in ('145400401','145400402', '148000301', '148000302')
 	and trim(ccc.cta_cmf) in ( '148000301', '148000302')
  and trim(d00.fl_ope_reneg)  <> 'C' --(Reneg Castigo) 
  """

# COMMAND ----------

sql_safe(query_ins_ope_tc)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 6: Actualizacion de Valores para modelo Interno de Tarjeta de Creditos (32) 

# COMMAND ----------

# MAGIC %md
# MAGIC Se insertan operaciones de Consumo utilizando tablas tmp_colocaciones_consumo generada en Paso 1 y tmp_modelo_consumo, Tabla de Salida de Notebook 005 (Operaciones Modelo interno) respectivamente para tipo Activo 32
# MAGIC
# MAGIC **Tabla de salida**: tmp_colocaciones_consumo_2 (Temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera 
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Subtipo cartera
# MAGIC - **mto_deuda_ope**: Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cta_ifrs**: Cuenta IFRS
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **nombre_activo**: Nombre Activo Cartera
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada
# MAGIC - **mto_provision**: Prov Oficial
# MAGIC - **cod_tipo_cli**: Tipo Cliente GR/IND
# MAGIC - **mto_exposicion**: Prov Oficial
# MAGIC - **factor_expo**: Porcentaje Factor Exposicion
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **pct_ltv**: relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado
# MAGIC - **fec_proceso**: Fecha Cierre

# COMMAND ----------

query_ins_ope_tc2 = """ insert into """+db_plat_tempX+""".tmp_colocaciones_consumo_2  
select a.periodo_id
	,a.rut_cliente
  ,a.dv_cliente
	,a.cod_ope_original
  ,a.cod_num_operacion
  ,a.cod_tip_cart 
	,a.cod_tipo_ope
	,a.des_producto_ope
  ,a.des_banca_ope
	,a.cod_cartera_ope
	,a.mnt_deuda_ope
  ,a.num_dias_mora_ope
  ,a.ind_cdet
  ,a.fec_ingreso_deteriodo_ope
  ,a.cod_cctb
  ,a.cod_cta_ifrs
  ,a.fl_ope_reneg 
	,a.cod_tipo_activo
	,a.nombre_activo 
	,a.pct_pi
  ,a.pct_pdi
  ,a.pct_pe
	,a.mnt_provision as mto_provision
	,a.cod_tipo_cli
	,a.mnt_exposicion
	,a.factor_expo
	,a.des_tipo_gtia as des_tipo_gtia				
	,a.mnt_garantia as mnt_garantia
	,a.pct_ltv as pct_ltv 
	,b.pct_pi as pct_pi_metodo_interno
  ,b.pct_pdi as  pct_pdi_metodo_interno
  ,b.pct_pe  as pct_pe_metodo_interno
	,b.mnt_prov_totalactiva as mto_PROV_METODO_INTERNO 
	,a.mnt_avalado
from """+db_plat_tempX+""".tmp_colocaciones_consumo a
inner join """+db_plat_tempX+""".tmp_modelo_consumo b
  on a.periodo_id = b.periodo_id
  and trim(a.cod_cartera_ope) = 'ACT'
  and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
  and trim(b.cod_modelo) = 'INT'
where a.periodo_id = """+anomes+""" 
	and a.cod_tipo_activo = 32 """

# COMMAND ----------

sql_safe(query_ins_ope_tc2)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 7: Actualizacion Valores Modelo Estandar Colocaciones Tarjeta de Credito (32) 

# COMMAND ----------

# MAGIC %md
# MAGIC Se insertan operaciones de Consumo utilizando tablas tmp_colocaciones_consumo generada en Paso 1 y tmp_modelo_consumo, Tabla de Salida de Notebook 005 (Operaciones Modelo Estandar) respectivamente para tipo Activo 32
# MAGIC
# MAGIC **Tabla de salida**: tmp_colocaciones_consumo_3 (Temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera 
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
# MAGIC - **factor_expo**: Porcentaje Factor Exposicion
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **pct_ltv**: relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado

# COMMAND ----------

query_ins_ope_tc3 = """ insert into """+db_plat_tempX+""".tmp_colocaciones_consumo_3
select a.periodo_id
	,a.rut_cliente
  ,a.dv_cliente
	,a.cod_ope_original
  ,a.cod_num_operacion
  ,a.cod_tip_cart 
	,a.cod_tipo_ope
	,a.des_producto_ope
  ,a.des_banca_ope
	,a.cod_cartera_ope
	,a.mnt_deuda_ope
  ,a.num_dias_mora_ope
  ,a.ind_cdet
  ,a.fec_ingreso_deteriodo_ope
  ,a.cod_cctb
  ,a.fl_ope_reneg 
	,a.cod_tipo_activo
	,a.nombre_activo 
	,b.pct_pi
  ,b.pct_pdi
  ,b.pct_pe
	,b.mnt_prov_totalactiva as mto_provision
	,a.cod_tipo_cli
	,a.mnt_exposicion
	,a.factor_expo
	,a.des_tipo_gtia as des_tipo_gtia				
	,a.mnt_garantia as mnt_garantia
	,a.pct_ltv as pct_ltv 
	,a.pct_pi as pct_pi_metodo_interno
  ,a.pct_pdi as  pct_pdi_metodo_interno
  ,a.pct_pe  as pct_pe_metodo_interno
	,a.mnt_prov_metodo_interno as mto_PROV_METODO_INTERNO 
	,a.mnt_avalado
from """+db_plat_tempX+""".tmp_colocaciones_consumo_2  a
left join """+db_plat_tempX+""".tmp_modelo_consumo b
  on a.periodo_id = b.periodo_id
  and trim(a.cod_cartera_ope) = 'ACT'
  and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
  and trim(b.cod_modelo) = 'STD'
where a.periodo_id = """+anomes+"""
  and a.cod_tipo_activo = 32
"""

# COMMAND ----------

sql_safe(query_ins_ope_tc3)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 8: Obtencion Colocaciones Leasing (33)

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de operaciones de consumo utilizando tablas tmp_d00_deuda_act_ctg (Salida de Notebook 1) para obtener Operaciones Leasing
# MAGIC
# MAGIC **Tabla de salida**: tmp_prestamo_hip_vivienda(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera 
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Subtipo cartera
# MAGIC - **mto_deuda_ope**: Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cta_ifrs**: Cuenta IFRS
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **nombre_activo**: Nombre Activo Cartera
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada
# MAGIC - **mto_provision**: Prov Oficial
# MAGIC - **cod_tipo_cli**: Tipo Cliente GR/IND
# MAGIC - **mto_exposicion**: Prov Oficial
# MAGIC - **factor_expo**: PI
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **pct_ltv**:relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado

# COMMAND ----------

query_ins_ope_lea = """ insert into """+db_plat_tempX+""".tmp_colocaciones_consumo  
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
	,33 as cod_tipo_activo
	,'LEASING CONSUMO' as nombre_activo 
	,0 as pct_pi
	,0 as pct_pdi
	,0 as pct_pe
	,0 as mto_provision
	,'GR' as cod_tipo_cli
	,0 as mto_exposicion
	,0.0 as factor_expo
	,'No_Tiene' as TIPO_GARANTIA				
	,0.0 as MONTO_GARANTIA
	,0 as PTVG 
	,0 as pct_pi_metodo_interno	
	,0 as pct_pdi_metodo_interno	
	,0 as pct_pe_metodo_interno	
	,0 as mto_PROV_METODO_INTERNO 
	,0 as mto_avalado
from """+db_plat_tempX+""".tmp_d00_deuda_act_ctg d00
where d00.periodo_id =  """+anomes+"""
	and trim(d00.cod_cartera_ope) = 'ACT'
 	and trim(d00.des_banca_ope) = 'LEASING'  
	and trim(d00.des_producto_ope) ='CONSUMO' """

# COMMAND ----------

sql_safe(query_ins_ope_lea)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 9: Actualizacion de Valores para modelo Interno de Leasing (33) 

# COMMAND ----------

# MAGIC %md
# MAGIC Se insertan operaciones de Consumo utilizando tablas tmp_colocaciones_consumo generada en Paso 1 y tmp_modelo_consumo, Tabla de Salida de Notebook 005 (Operaciones Modelo interno) respectivamente para tipo Activo 33
# MAGIC
# MAGIC **Tabla de salida**: tmp_colocaciones_consumo_2 (Temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera 
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Subtipo cartera
# MAGIC - **mto_deuda_ope**: Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cta_ifrs**: Cuenta IFRS
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **nombre_activo**: Nombre Activo Cartera
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada
# MAGIC - **mto_provision**: Prov Oficial
# MAGIC - **cod_tipo_cli**: Tipo Cliente GR/IND
# MAGIC - **mto_exposicion**: Prov Oficial
# MAGIC - **factor_expo**: Porcentaje Factor Exposicion
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **pct_ltv**: relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado
# MAGIC - **fec_proceso**: Fecha Cierre

# COMMAND ----------

query_ins_ope_lea2 = """ insert into """+db_plat_tempX+""".tmp_colocaciones_consumo_2  
select a.periodo_id
	,a.rut_cliente
  ,a.dv_cliente
	,a.cod_ope_original
  ,a.cod_num_operacion
  ,a.cod_tip_cart 
	,a.cod_tipo_ope
	,a.des_producto_ope
  ,a.des_banca_ope
	,a.cod_cartera_ope
	,a.mnt_deuda_ope
  ,a.num_dias_mora_ope
  ,a.ind_cdet
  ,a.fec_ingreso_deteriodo_ope
  ,a.cod_cctb
  ,a.cod_cta_ifrs
  ,a.fl_ope_reneg 
	,a.cod_tipo_activo
	,a.nombre_activo 
	,b.pct_pi
  ,b.pct_pdi
  ,b.pct_pe
	,a.mnt_provision as mto_provision
	,a.cod_tipo_cli
	,a.mnt_exposicion
	,a.factor_expo
	,a.des_tipo_gtia as des_tipo_gtia				
	,a.mnt_garantia as mnt_garantia
	,a.pct_ltv as pct_ltv 
	,b.pct_pi as pct_pi_metodo_interno
  ,b.pct_pdi as  pct_pdi_metodo_interno
  ,b.pct_pe  as pct_pe_metodo_interno
	,b.mnt_prov_totalactiva as mto_PROV_METODO_INTERNO 
	,a.mnt_avalado
from """+db_plat_tempX+""".tmp_colocaciones_consumo a
left join """+db_plat_tempX+""".tmp_modelo_consumo b
  on a.periodo_id = b.periodo_id
  and trim(a.cod_cartera_ope) = 'ACT'
  and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
  and trim(b.cod_modelo) = 'INT'
where a.periodo_id = """+anomes+""" 
	and a.cod_tipo_activo = 33 """

# COMMAND ----------

sql_safe(query_ins_ope_lea2)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 10: Actualizacion Valores Modelo Estandar Colocaciones Tarjeta de Credito (33) 

# COMMAND ----------

# MAGIC %md
# MAGIC Se insertan operaciones de Consumo utilizando tablas tmp_colocaciones_consumo generada en Paso 1 y tmp_modelo_consumo, Tabla de Salida de Notebook 005 (Operaciones Modelo Estandar) respectivamente para tipo Activo 33
# MAGIC
# MAGIC **Tabla de salida**: tmp_colocaciones_consumo_3 (Temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera 
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
# MAGIC - **factor_expo**: Porcentaje Factor Exposicion
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **pct_ltv**: relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado

# COMMAND ----------

query_ins_ope_lea3 = """ insert into """+db_plat_tempX+""".tmp_colocaciones_consumo_3
select a.periodo_id
	,a.rut_cliente
  ,a.dv_cliente
	,a.cod_ope_original
  ,a.cod_num_operacion
  ,a.cod_tip_cart 
	,a.cod_tipo_ope
	,a.des_producto_ope
  ,a.des_banca_ope
	,a.cod_cartera_ope
	,a.mnt_deuda_ope
  ,a.num_dias_mora_ope
  ,a.ind_cdet
  ,a.fec_ingreso_deteriodo_ope
  ,a.cod_cctb
  ,a.fl_ope_reneg 
	,a.cod_tipo_activo
	,a.nombre_activo 
	,b.pct_pi
  ,b.pct_pdi
  ,b.pct_pe
	,b.mnt_prov_totalactiva as mto_provision
	,a.cod_tipo_cli
	,a.mnt_exposicion
	,a.factor_expo
	,a.des_tipo_gtia as des_tipo_gtia				
	,a.mnt_garantia as mnt_garantia
	,a.pct_ltv as pct_ltv 
	,a.pct_pi as pct_pi_metodo_interno
  ,a.pct_pdi as  pct_pdi_metodo_interno
  ,a.pct_pe  as pct_pe_metodo_interno
	,a.mnt_prov_metodo_interno as mto_PROV_METODO_INTERNO 
	,a.mnt_avalado
from """+db_plat_tempX+""".tmp_colocaciones_consumo_2  a
inner join """+db_plat_tempX+""".tmp_modelo_consumo b
  on a.periodo_id = b.periodo_id
  and trim(a.cod_cartera_ope) = 'ACT'
  and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
  and trim(b.cod_modelo) = 'STD'
where a.periodo_id = """+anomes+"""
  and a.cod_tipo_activo = 33
"""

# COMMAND ----------

sql_safe(query_ins_ope_lea3)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 11: Obtencion Colocaciones Lineas de Credito (34)

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de operaciones de consumo utilizando tablas tmp_d00_deuda_act_ctg (Salida de Notebook 1) para obtener Operaciones de Linea
# MAGIC
# MAGIC **Colocaciones de Consumo**
# MAGIC - **148000200**:	Deudores en cuentas corrientes
# MAGIC
# MAGIC **Tabla de salida**: tmp_prestamo_hip_vivienda(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera 
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Subtipo cartera
# MAGIC - **mto_deuda_ope**: Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cta_ifrs**: Cuenta IFRS
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **nombre_activo**: Nombre Activo Cartera
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada
# MAGIC - **mto_provision**: Prov Oficial
# MAGIC - **cod_tipo_cli**: Tipo Cliente GR/IND
# MAGIC - **mto_exposicion**: Prov Oficial
# MAGIC - **factor_expo**: PI
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **pct_ltv**:relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado

# COMMAND ----------

query_ins_ope_lc = """ insert into """+db_plat_tempX+""".tmp_colocaciones_consumo  
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
	,trim(ccc.cta_cmf) as cod_cta_ifrs
  ,d00.fl_ope_reneg 
	,34 as cod_tipo_activo
	,'LINEAS CONSUMO' as nombre_activo 
	,0 as pct_pi
	,0 as pct_pdi
	,0 as pct_pe
	,0 as mto_provision
	,'GR' as cod_tipo_cli
	,0 as mto_exposicion
	,0.0 as factor_expo
	,'No_Tiene' as TIPO_GARANTIA				
	,0.0 as MONTO_GARANTIA
	,0 as PTVG 
	,0 as pct_pi_metodo_interno	
	,0 as pct_pdi_metodo_interno	
	,0 as pct_pe_metodo_interno	
	,0 as mto_PROV_METODO_INTERNO 
	,0 as mto_avalado
from """+db_plat_tempX+""".tmp_d00_deuda_act_ctg d00
inner join """+db_slv_ParametricasX+""".TBU ccc
		on trim(d00.cod_cctb) = trim(ccc.cta_ctable) 
  	and d00.periodo_id = substring(ccc.fecha_informada, 1, 6)
where d00.periodo_id =  """+anomes+"""
	and trim(d00.cod_cartera_ope) = 'ACT'
 	and trim(ccc.cta_cmf) IN ('148000200') /*'145400300','148000901',*/
  and ccc.fecha_informada = """+str(dia_tbu)+"""
  and trim(d00.fl_ope_reneg)  <> 'C' --(Reneg Castigo)  
  """

# COMMAND ----------

sql_safe(query_ins_ope_lc)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 12: Actualizacion de Valores para modelo Interno de Lineas (34) 

# COMMAND ----------

# MAGIC %md
# MAGIC Se insertan operaciones de Consumo utilizando tablas tmp_colocaciones_consumo generada en Paso 1 y tmp_modelo_consumo, Tabla de Salida de Notebook 005 (Operaciones Modelo interno) respectivamente para tipo Activo 34
# MAGIC
# MAGIC **Tabla de salida**: tmp_colocaciones_consumo_2 (Temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera 
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Subtipo cartera
# MAGIC - **mto_deuda_ope**: Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cta_ifrs**: Cuenta IFRS
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **nombre_activo**: Nombre Activo Cartera
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada
# MAGIC - **mto_provision**: Prov Oficial
# MAGIC - **cod_tipo_cli**: Tipo Cliente GR/IND
# MAGIC - **mto_exposicion**: Prov Oficial
# MAGIC - **factor_expo**: Porcentaje Factor Exposicion
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **pct_ltv**: relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado
# MAGIC - **fec_proceso**: Fecha Cierre

# COMMAND ----------

query_ins_ope_lc2 = """ insert into """+db_plat_tempX+""".tmp_colocaciones_consumo_2  
select a.periodo_id
	,a.rut_cliente
  ,a.dv_cliente
	,a.cod_ope_original
  ,a.cod_num_operacion
  ,a.cod_tip_cart 
	,a.cod_tipo_ope
	,a.des_producto_ope
  ,a.des_banca_ope
	,a.cod_cartera_ope
	,a.mnt_deuda_ope
  ,a.num_dias_mora_ope
  ,a.ind_cdet
  ,a.fec_ingreso_deteriodo_ope
  ,a.cod_cctb
  ,a.cod_cta_ifrs
  ,a.fl_ope_reneg 
	,a.cod_tipo_activo
	,a.nombre_activo 
	,b.pct_pi
  ,b.pct_pdi
  ,b.pct_pe
	,a.mnt_provision as mto_provision
	,a.cod_tipo_cli
	,a.mnt_exposicion
	,a.factor_expo
	,a.des_tipo_gtia as des_tipo_gtia				
	,a.mnt_garantia as mnt_garantia
	,a.pct_ltv as pct_ltv 
	,b.pct_pi as pct_pi_metodo_interno
  ,b.pct_pdi as  pct_pdi_metodo_interno
  ,b.pct_pe  as pct_pe_metodo_interno
	,b.mnt_prov_totalactiva as mto_PROV_METODO_INTERNO 
	,a.mnt_avalado
from """+db_plat_tempX+""".tmp_colocaciones_consumo a
left join """+db_plat_tempX+""".tmp_modelo_consumo b
  on a.periodo_id = b.periodo_id
  and trim(a.cod_cartera_ope) = 'ACT'
  and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
  and trim(b.cod_modelo) = 'INT'
where a.periodo_id = """+anomes+""" 
	and a.cod_tipo_activo = 34 """

# COMMAND ----------

sql_safe(query_ins_ope_lc2)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 13: Actualizacion Valores Modelo Estandar Colocaciones Lineas (34) 

# COMMAND ----------

# MAGIC %md
# MAGIC Se insertan operaciones de Consumo utilizando tablas tmp_colocaciones_consumo generada en Paso 1 y tmp_modelo_consumo, Tabla de Salida de Notebook 005 (Operaciones Modelo Estandar) respectivamente para tipo Activo 34
# MAGIC
# MAGIC **Tabla de salida**: tmp_colocaciones_consumo_3 (Temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera 
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
# MAGIC - **factor_expo**: Porcentaje Factor Exposicion
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **pct_ltv**: relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado

# COMMAND ----------

query_ins_ope_lc3 = """ insert into """+db_plat_tempX+""".tmp_colocaciones_consumo_3
select a.periodo_id
	,a.rut_cliente
  ,a.dv_cliente
	,a.cod_ope_original
  ,a.cod_num_operacion
  ,a.cod_tip_cart 
	,a.cod_tipo_ope
	,a.des_producto_ope
  ,a.des_banca_ope
	,a.cod_cartera_ope
	,a.mnt_deuda_ope
  ,a.num_dias_mora_ope
  ,a.ind_cdet
  ,a.fec_ingreso_deteriodo_ope
  ,a.cod_cctb
  ,a.fl_ope_reneg 
	,a.cod_tipo_activo
	,a.nombre_activo 
	,b.pct_pi
  ,b.pct_pdi
  ,b.pct_pe
	,b.mnt_prov_totalactiva as mto_provision
	,a.cod_tipo_cli
	,a.mnt_exposicion
	,a.factor_expo
	,a.des_tipo_gtia as des_tipo_gtia				
	,a.mnt_garantia as mnt_garantia
	,a.pct_ltv as pct_ltv 
	,a.pct_pi as pct_pi_metodo_interno
  ,a.pct_pdi as  pct_pdi_metodo_interno
  ,a.pct_pe  as pct_pe_metodo_interno
	,a.mnt_prov_metodo_interno as mto_PROV_METODO_INTERNO 
	,a.mnt_avalado
from """+db_plat_tempX+""".tmp_colocaciones_consumo_2  a
left join """+db_plat_tempX+""".tmp_modelo_consumo b
  on a.periodo_id = b.periodo_id
  and trim(a.cod_cartera_ope) = 'ACT'
  and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
  and trim(b.cod_modelo) = 'STD'
where a.periodo_id = """+anomes+"""
  and a.cod_tipo_activo = 34
"""

# COMMAND ----------

sql_safe(query_ins_ope_lc3)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 14: Insert Colocaciones de Consumo

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

query_ins_consumo = """ insert into """ + db_platinumX + """.base_archivos_normativos
select periodo_id
	,rut_cliente
	,dv_cliente
	,cod_ope_original
 	,CASE WHEN substring(cod_tipo_ope, 1,3)='LEA'THEN 'LEA'|| SUBSTRING(TRIM(cod_num_operacion),4,15) ELSE TRIM(cod_num_operacion) END  as cod_num_operacion 
	,cod_tip_cart
	,cod_tipo_ope
	,des_producto_ope
	,des_banca_ope
	,cod_cartera_ope
	,mnt_deuda_ope
	,num_dias_mora_ope
	,ind_cdet
	,fec_ingreso_deteriodo_ope
	,cod_cctb
	,fl_ope_reneg
 	,-998                              as cod_tabla_34
	,cod_tipo_activo
	,nombre_activo
	,pct_pi
	,pct_pdi
	,pct_pe
	,mnt_provision
	,cod_tipo_cli
	,mnt_exposicion
	,factor_expo
	,des_tipo_gtia
	,mnt_garantia
	,pct_ltv
	,pct_pi_metodo_interno
	,pct_pdi_metodo_interno
	,pct_pe_metodo_interno
	,mnt_prov_metodo_interno
	,mnt_avalado
 ,'"""+str(date_proceso)+"""'
from """+db_plat_tempX+""".tmp_colocaciones_consumo_3  
where periodo_id = """+anomes+"""
 """

# COMMAND ----------

sql_safe(query_ins_consumo)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Conteo de registros insertados en tabla de salida

# COMMAND ----------

registros = spark.sql(""" select count(1) as cant_registros from """ + db_platinumX + """.base_archivos_normativos  where periodo_id = """+anomes+"""  and cod_tabla_89 in (31, 32, 33, 34) """)

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Insertaron " + str(cantidad) +" Registros en la tabla temporal base_archivos_normativos ")

# COMMAND ----------

# MAGIC %md
# MAGIC # Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")