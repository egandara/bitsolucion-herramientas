# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_012_Base_Normativa_Operaciones_Estudiantiles

# COMMAND ----------

# MAGIC %md
# MAGIC ## Información del Notebook

# COMMAND ----------

# MAGIC %md
# MAGIC ### Encabezado
# MAGIC **************************************************************************
# MAGIC * Nombre: BCI_012_Base_Normativa_Operaciones_Estudiantiles.ipynb
# MAGIC * Ruta: 
# MAGIC * Autor:  BitSolucion Spa
# MAGIC * Ing. SW BCI: <alguien.de.ti@bci.cl> - Marcelo Lizama <marcelo.lizama@bci.cl> - Ivan Jara <ivan.jara@bci.cl>
# MAGIC * Fecha: 22/12/2025
# MAGIC * Descripción: Notebook encargado de insertar información de Operaciones Estudiantiles CAE, Corfo y otros Creditos de Educación, Tipos de Activo, 14 Cae Activos, 15 Corfo, 16 Otros Creditos y 61 Cae Contingente
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
# MAGIC * tmp_modelo_com_gr
# MAGIC * dsr_slv_Parametricas_db.tbu
# MAGIC ***************************************************************************
# MAGIC #### Tabla Salida Temporal: 
# MAGIC * base_archivos_normativos (Activo 14, 15, 16, 61 Tabla 89 Manual CMF)

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
# MAGIC ### Tabla tmp_prestamo_estudiantil

# COMMAND ----------

paso_tb_del_tmp_prestamo_estudiantil  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_prestamo_estudiantil"""

# COMMAND ----------

sql_safe(paso_tb_del_tmp_prestamo_estudiantil)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_prestamo_estudiantil", True)

# COMMAND ----------

paso_tb_crea_tmp_prestamo_estudiantil  = """CREATE TABLE """ + db_plat_tempX + """.tmp_prestamo_estudiantil
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
	mnt_deuda_ope	   DECIMAL(18,0) COMMENT 'Saldo total ifrs',
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
	mnt_prov_metodo_interno  DECIMAL(18,4) COMMENT 'Provisión de la Colocación',
	mnt_avalado DECIMAL(32,0) COMMENT 'MONTO_AVALADO'
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_prestamo_estudiantil' """

# COMMAND ----------

sql_safe(paso_tb_crea_tmp_prestamo_estudiantil)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Desarrollo Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 1: Obtencion Créditos para estudios superiores ley N° 20.027 (CAE), CORFO y/o Otros Créditos Estudiantiles

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de operaciones Estudiantiles (CAE, Corfo y Otros créditos para estudios superiores)  utilizando tablas tmp_d00_deuda_act_ctg (Salida de Notebook 1)y tabla TBU para identificar cuenta Ifrs
# MAGIC
# MAGIC **Colocacion Comercial Préstamos estudiantiles**
# MAGIC - **145400701**: Créditos para estudios superiores Ley N° 20.027 (CAE)
# MAGIC - **145400702**: Créditos con garantía CORFO
# MAGIC - **145400703**: Otros créditos para estudios superiores
# MAGIC
# MAGIC **Tabla de salida**: tmp_prestamo_estudiantil(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera (OPE_VIGENTE)**: ,
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
# MAGIC - **pct_pi**: PI
# MAGIC - **pct_pdi**: PDI
# MAGIC - **pct_pe**: PE
# MAGIC - **mto_provision**: Prov Oficial
# MAGIC - **cod_tipo_cli**: Tipo Cliente GR/IND
# MAGIC - **mto_exposicion**: Prov Oficial
# MAGIC - **factor_expo**: PI
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: MONTO_GARANTIA' ,
# MAGIC - **pct_ltv**: PTVG
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: MONTO_AVALADO

# COMMAND ----------

diaHabil = spark.sql("""select max(fecha_informada) as periodo_id  FROM  """+ db_slv_ParametricasX +""".TBU  
where substring(fecha_informada, 1, 6) = """+ anomes +"""  """)

ultimoDiaHabil_tbu = diaHabil.toPandas()
dia_tbu = ultimoDiaHabil_tbu.iloc[0]["periodo_id"]
 
print(dia_tbu)

# COMMAND ----------

query_ins_cae = """ insert into """+db_plat_tempX+""".tmp_prestamo_estudiantil  
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
	,14 as cod_tipo_activo
	,'CRED. ESTUDIANTIL CAE' as nombre_activo 
	,0 as pct_pi
	,0 as pct_pdi
	,0 as pct_pe
	,0 as mto_provision
	,'GR' as cod_tipo_cli
	,0 as mto_exposicion
	,0.0 as factor_expo
	,'' as TIPO_GARANTIA				
	,0 as MONTO_GARANTIA
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
 """

# COMMAND ----------

sql_safe(query_ins_cae)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 2: Eliminación Creditos Estudiantiles por Tipo de Activo para fecha de Proceso

# COMMAND ----------

query_delete_est = """DELETE FROM """ + db_platinumX + """.base_archivos_normativos WHERE periodo_id = """+anomes+""" and cod_tabla_89 in (14, 15, 16, 61) """

# COMMAND ----------

sql_safe(query_delete_est)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 3: Insert Operaciones Activas de Creditos Estudiantiles

# COMMAND ----------

# MAGIC %md
# MAGIC Se insertan operaciones Estudiantiles Activas con Creditos Estudiantiles utilizando tablas tmp_prestamo_estudiantil generada en Paso 1y tmp_modelo_com_gr, Tabla de Salida de Notebook3 respectivamente
# MAGIC
# MAGIC **Colocacion Comercial Préstamos estudiantiles**
# MAGIC - **145400701**: Créditos para estudios superiores Ley N° 20.027 (CAE)
# MAGIC - **145400702**: Créditos con garantía CORFO
# MAGIC - **145400703**: Otros créditos para estudios superiores
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
# MAGIC - **pct_pi**: PI
# MAGIC - **pct_pdi**: PDI
# MAGIC - **pct_pe**: PE
# MAGIC - **mto_provision**: Prov Oficial
# MAGIC - **cod_tipo_cli**: Tipo Cliente GR/IND
# MAGIC - **mto_exposicion**: Prov Oficial
# MAGIC - **factor_expo**: PI
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: MONTO_GARANTIA' ,
# MAGIC - **pct_ltv**: PTVG
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: MONTO_AVALADO

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

query_ins_estudiantil_act = """ insert into """ + db_platinumX + """.base_archivos_normativos
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
  ,-998                              as cod_tabla_34
	,case when trim(a.cod_cta_ifrs) = '145400701' then 14
				when trim(a.cod_cta_ifrs) = '145400702' then 15
				when trim(a.cod_cta_ifrs) = '145400703' then 16
	else a.cod_tipo_activo  end as cod_tipo_activo
	,case when trim(a.cod_cta_ifrs) = '145400701' then 'CRED. ESTUDIANTIL CAE'
				when trim(a.cod_cta_ifrs) = '145400702' then 'CRED. CORFO'
				when trim(a.cod_cta_ifrs) = '145400703' then 'OTROS CRED. ESTUDIANTILES'
	else a.nombre_activo  end as nombre_activo 
	,b.pct_pi
  ,b.pct_pdi
  ,b.pct_pe
	,b.mnt_prov_oficial as mto_provision
	,a.cod_tipo_cli
	,a.mnt_exposicion
	,a.factor_expo
	,b.des_tipo_gtia as des_tipo_gtia				
	,b.mnt_garantia as mnt_garantia
	,b.pct_ltv as pct_ltv 
	,b.pct_pi_metodo_interno as pct_pi_metodo_interno
  ,b.pct_pdi_metodo_interno as  pct_pdi_metodo_interno
  ,b.pct_pe_metodo_interno  as pct_pe_metodo_interno
	,b.mnt_prov_int as mto_prov_metodo_interno 
	,ROUND(b.mnt_avalado,0) as mto_avalado
 ,'"""+str(date_proceso)+"""'
from """+db_plat_tempX+""".tmp_prestamo_estudiantil  a
inner join """+db_plat_tempX+""".tmp_modelo_com_gr b
  on a.periodo_id = b.periodo_id
  and trim(a.cod_cartera_ope) = trim(b.cod_stp_cart)
  and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
where a.periodo_id = """+anomes+"""
  and trim(a.cod_cta_ifrs) in ('145400701', '145400702', '145400703') """

# COMMAND ----------

sql_safe(query_ins_estudiantil_act)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 4: Insert Operaciones Contingentes de Creditos Estudiantiles

# COMMAND ----------

# MAGIC %md
# MAGIC Se insertan operaciones Estudiantiles Contingentes con Creditos para Cuentas Contables correspondientes a operaciones CAE utilizando tablas tmp_prestamo_estudiantil generada en Paso 1y tmp_modelo_com_gr, Tabla de Salida de Notebook3 respectivamente
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
# MAGIC - **pct_pi**: PI
# MAGIC - **pct_pdi**: PDI
# MAGIC - **pct_pe**: PE
# MAGIC - **mto_provision**: Prov Oficial
# MAGIC - **cod_tipo_cli**: Tipo Cliente GR/IND
# MAGIC - **mto_exposicion**: Prov Oficial
# MAGIC - **factor_expo**: PI
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: MONTO_GARANTIA' ,
# MAGIC - **pct_ltv**: PTVG
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: MONTO_AVALADO

# COMMAND ----------

query_ins_est_ctg = """ insert into """ + db_platinumX + """.base_archivos_normativos 
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
  ,d00.fl_ope_reneg 
  ,-998                              as cod_tabla_34
	,61 as cod_tipo_activo
	,'CAE CONTINGENTE' as nombre_activo 
	,b.pct_pi
  ,b.pct_pdi
  ,b.pct_pe
	,b.mnt_prov_oficial as mto_provision
	,'GR' as cod_tipo_cli
	,round(
   (CASE WHEN trim(d00.ind_cdet) = 'N' OR (trim(d00.ind_cdet) = 'D' AND b.pct_pi <> 1) THEN (d00.mto_deuda_ope * 0.15) 
    ELSE 0 END),0) as mto_exposicion
	,0.15 as factor_expo
	,b.des_tipo_gtia as TIPO_GARANTIA				
	,b.mnt_garantia as MONTO_GARANTIA
	,b.pct_ltv as PTVG 
	,b.pct_pi_metodo_interno as pct_pi_metodo_interno
  ,b.pct_pdi_metodo_interno as  pct_pdi_metodo_interno
  ,b.pct_pe_metodo_interno  as pct_pe_metodo_interno
	,b.mnt_prov_int as mto_PROV_METODO_INTERNO 
	,ROUND(b.mnt_avalado,0) as mto_avalado
 ,'"""+str(date_proceso)+"""'
from """+db_plat_tempX+""".tmp_d00_deuda_act_ctg d00
inner join """+db_plat_tempX+""".tmp_modelo_com_gr b
  on d00.periodo_id = b.periodo_id
  and trim(d00.cod_cartera_ope) = trim(b.cod_stp_cart)
  and trim(d00.cod_num_operacion) = trim(b.cod_num_operacion)
where d00.periodo_id =  """+anomes+"""
	and trim(d00.cod_cartera_ope) = 'CTG'
  and trim(d00.fl_ope_reneg) <> 'C' --(Reneg prov Castigo)	
	and trim(d00.cod_cctb) in ('931000026','931000027')
 """

# COMMAND ----------

sql_safe(query_ins_est_ctg)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Conteo de registros insertados en tabla de salida

# COMMAND ----------

registros = spark.sql(""" select count(1) as cant_registros from """ + db_platinumX + """.base_archivos_normativos  where periodo_id = """+anomes+""" and cod_tabla_89 in (14, 15, 16, 61) """)

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Insertaron " + str(cantidad) +" Registros en la tabla temporal base_archivos_normativos ")

# COMMAND ----------

# MAGIC %md
# MAGIC # Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")